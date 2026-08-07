using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
// JsonDefaults lives in the root Prose.Core namespace.
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Faction + 8 child
/// tables) and the domain model (FactionData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — FactionRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert — same shape as CharacterMapper.
/// Faction-level tags live in the universal EntityTags layer (same as
/// CharacterRepository). Relationship tags live in the new
/// FactionRelationshipTags bridge.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here. The blob
/// column carries the data until the human retires it after a parity gate.
/// </summary>
public static class FactionMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns one <see cref="FactionData"/>
    /// per active faction with only the fields the list view needs: Id, Name,
    /// Slug (via Entity), Rating, VoteCount, and Tags. No Includes, no bridge
    /// materialization.
    /// </summary>
    public static List<FactionData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Factions.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "faction"),
                f => f.Id, e => e.Id,
                (f, e) => new { Id = f.Id, Name = e.Name, f.Rating, f.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<FactionData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new FactionData
            {
                Id       = r.Id.ToString("N"),
                Type     = "faction",
                Name     = r.Name ?? "",
                Rating   = r.Rating,
                VoteCount = r.VoteCount,
                Tags     = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Faction row + all child collections,
    /// then project to FactionData. Records.Json is never read here.
    /// </summary>
    public static List<FactionData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "faction")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "faction" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var factions = BuildIncludeChain(db.Factions.AsNoTracking())
            .Where(f => ids.Contains(f.Id))
            .ToList();

        var entityById = db.Entities.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionary(e => e.Id, e => e);

        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<FactionData>(factions.Count);
        foreach (var f in factions)
        {
            entityById.TryGetValue(f.Id, out var entity);
            tagsByEntity.TryGetValue(f.Id, out var tags);
            result.Add(Materialize(f, entity, tags));
        }
        return result;
    }

    /// <summary>
    /// Load a single faction by id, including all bridges. Returns null when
    /// not found.
    /// </summary>
    public static FactionData? LoadOne(ProseDbContext db, Guid id)
    {
        var f = BuildIncludeChain(db.Factions.AsNoTracking())
            .FirstOrDefault(f => f.Id == id);
        if (f == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(f, entity, tags);
    }

    private static IQueryable<Faction> BuildIncludeChain(IQueryable<Faction> q)
        => q.AsSplitQuery()
            .Include(f => f.Aliases)
            .Include(f => f.Methods)
            .Include(f => f.Resources)
            .Include(f => f.Goals)
            .Include(f => f.StoryHooks)
            .Include(f => f.Relationships).ThenInclude(r => r.Tags)
            .Include(f => f.Members);

    /// <summary>
    /// Build a FactionData from the entity + bridges loaded by BuildIncludeChain.
    /// Entity is used for the universal Name/Slug; every other field comes from
    /// the columnar Faction row.
    /// </summary>
    public static FactionData Materialize(Faction f, Entity? entity, List<string>? tags)
    {
        var data = new FactionData
        {
            Id                = f.Id.ToString("N"),
            Type              = "faction",
            Name              = entity?.Name ?? f.Name,
            Rating            = f.Rating,
            VoteCount         = f.VoteCount,
            Motto             = f.Motto,
            Description       = f.Description,
            Ideology          = f.Ideology,
            Territory         = f.Territory,
            Leadership        = f.Leadership,
            NarrativeFunction = f.NarrativeFunction,
            MidjourneyPrompt  = f.MidjourneyPrompt,
            Dalle3Prompt      = f.Dalle3Prompt,
            Tags              = tags ?? new List<string>(),
        };

        data.Aliases    = f.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.Methods    = f.Methods.OrderBy(x => x.Position).Select(x => x.Method).ToList();
        data.Resources  = f.Resources.OrderBy(x => x.Position).Select(x => x.Resource).ToList();
        data.Goals      = f.Goals.OrderBy(x => x.Position).Select(x => x.Goal).ToList();
        data.StoryHooks = f.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        data.Relationships = f.Relationships.OrderBy(x => x.Position).Select(r => new FactionRelationship
        {
            Name        = r.Alias,
            Type        = r.RelationshipType,
            Description = r.Description,
            Tags        = r.Tags.OrderBy(t => t.Position).Select(t => t.Value).ToList(),
        }).ToList();

        data.KnownMembers = f.Members.OrderBy(x => x.Position).Select(m => new FactionMember
        {
            Name   = m.Alias,
            Role   = m.Role,
            Status = m.MemberStatus,
            Notes  = m.Notes,
        }).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a FactionData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted (same pattern as CharacterMapper.PersistAsync).
    /// Caller must call db.SaveChanges() / db.SaveChangesAsync() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, FactionData src, CancellationToken ct = default)
    {
        var faction = await db.Factions.FirstOrDefaultAsync(f => f.Id == id, ct);
        var isNew = faction == null;

        if (!isNew)
        {
            // Wipe all bridges — cascade deletes handle grandchildren (e.g.
            // FactionRelationshipTags cascade via FactionRelationships FK).
            await db.FactionAliases.Where(x => x.FactionId == id).ExecuteDeleteAsync(ct);
            await db.FactionMethods.Where(x => x.FactionId == id).ExecuteDeleteAsync(ct);
            await db.FactionResources.Where(x => x.FactionId == id).ExecuteDeleteAsync(ct);
            await db.FactionGoals.Where(x => x.FactionId == id).ExecuteDeleteAsync(ct);
            await db.FactionStoryHooks.Where(x => x.FactionId == id).ExecuteDeleteAsync(ct);
            await db.FactionRelationships.Where(x => x.FactionId == id).ExecuteDeleteAsync(ct);
            await db.FactionMembers.Where(x => x.FactionId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            faction = new Faction { Id = id };
            db.Factions.Add(faction);
        }

        FillScalars(faction!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Faction from src (no DB touch).</summary>
    public static void FillScalars(Faction f, FactionData src)
    {
        f.Name              = src.Name ?? "";
        f.Rating            = src.Rating;
        f.VoteCount         = src.VoteCount;
        f.Motto             = src.Motto ?? "";
        f.Description       = src.Description ?? "";
        f.Ideology          = src.Ideology ?? "";
        f.Territory         = src.Territory ?? "";
        f.Leadership        = src.Leadership ?? "";
        f.NarrativeFunction = src.NarrativeFunction ?? "";
        f.MidjourneyPrompt  = src.MidjourneyPrompt ?? "";
        f.Dalle3Prompt      = src.Dalle3Prompt ?? "";
        // Sector / Tier / Allegiance are indexed classification columns that
        // FactionData has no corresponding fields for; leave them unchanged on
        // update so they don't get clobbered by a round-trip through the domain model.
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, FactionData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.FactionAliases.Add(new FactionAlias { FactionId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.Methods.Count; i++)
            db.FactionMethods.Add(new FactionMethod { FactionId = id, Position = i, Method = src.Methods[i] ?? "" });

        for (int i = 0; i < src.Resources.Count; i++)
            db.FactionResources.Add(new FactionResource { FactionId = id, Position = i, Resource = src.Resources[i] ?? "" });

        for (int i = 0; i < src.Goals.Count; i++)
            db.FactionGoals.Add(new FactionGoal { FactionId = id, Position = i, Goal = src.Goals[i] ?? "" });

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.FactionStoryHooks.Add(new FactionStoryHook { FactionId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });

        for (int i = 0; i < src.Relationships.Count; i++)
        {
            var rel = src.Relationships[i];
            var targetFactionId = ResolveEntityId(db, "faction", rel.Name);
            var relRow = new FactionRelationshipRow
            {
                FactionId        = id,
                Position         = i,
                Alias            = rel.Name ?? "",
                RelationshipType = rel.Type ?? "",
                Description      = rel.Description ?? "",
                TargetFactionId  = targetFactionId,
            };
            // Relationship tags are children of the relationship row. EF will
            // assign the FK after SaveChanges resolves the identity — add them
            // to the navigation collection so EF wires them correctly.
            for (int j = 0; j < rel.Tags.Count; j++)
                relRow.Tags.Add(new FactionRelationshipTag { Position = j, Value = rel.Tags[j] ?? "" });
            db.FactionRelationships.Add(relRow);
        }

        for (int i = 0; i < src.KnownMembers.Count; i++)
        {
            var m = src.KnownMembers[i];
            var characterId = ResolveEntityId(db, "character", m.Name);
            db.FactionMembers.Add(new FactionMemberRow
            {
                FactionId    = id,
                Position     = i,
                Alias        = m.Name ?? "",
                Role         = m.Role ?? "",
                MemberStatus = string.IsNullOrEmpty(m.Status) ? "active" : m.Status,
                Notes        = m.Notes ?? "",
                CharacterId  = characterId,
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active faction Entity, deserialize its Records.Json
    /// blob → FactionData → persist via FillScalars + FillBridges + sync
    /// EntityTags. Returns the number of factions written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once after the schema migration via <c>ss --rebuild-faction-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        // Fetch all faction Record rows in one shot.
        var factionEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "faction" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        if (factionEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => factionEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            FactionData? src;
            try { src = System.Text.Json.JsonSerializer.Deserialize<FactionData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "FactionMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
                continue;
            }
            if (src == null) continue;

            try
            {
                await PersistAsync(db, row.EntityId, src, ct);
                SyncTagsForEntity(db, row.EntityId, src.Tags);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "FactionMapper.RebuildAllAsync: failed to persist faction {Id}", row.EntityId);
                // Clear tracker so the next faction starts clean.
                db.ChangeTracker.Clear();
            }
        }
        return written;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Look up the canonical Entity id for a free-form name. Tries exact name
    /// match first, then slug. Returns null when nothing matches — the bridge
    /// keeps the alias string either way.
    /// </summary>
    private static Guid? ResolveEntityId(ProseDbContext db, string entityType, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = Prose.Core.Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.EntityType == entityType && e.IsActive
                && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Add any tag names that aren't already attached to this entity. The
    /// universal Tag/EntityTag tables are the source of truth — this only adds,
    /// matching the existing import behavior (tag removal is a manual op).
    /// Mirrors CharacterRepository.SyncTagsForEntity exactly.
    /// </summary>
    internal static void SyncTagsForEntity(ProseDbContext db, Guid entityId, IReadOnlyList<string>? tags)
    {
        if (tags == null || tags.Count == 0) return;
        var existing = db.EntityTags
            .Where(t => t.EntityId == entityId)
            .Select(t => t.Tag!.Name)
            .ToList()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var wanted = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(t => !existing.Contains(t))
            .ToList();
        if (wanted.Count == 0) return;

        var byName = db.Tags
            .Where(t => wanted.Contains(t.Name))
            .ToList()
            .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

        foreach (var tagName in wanted)
        {
            if (!byName.TryGetValue(tagName, out var tag))
            {
                tag = new Tag { Name = tagName };
                db.Tags.Add(tag);
                byName[tagName] = tag;
            }
            db.EntityTags.Add(new EntityTag { EntityId = entityId, Tag = tag });
        }
    }
}
