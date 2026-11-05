using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>What <see cref="EntityCreator.CreateAsync"/> did: the new entity's id and slug, the record as
/// read back from the database, and any warnings (a same-named entity of another type, a new repository).</summary>
public sealed record EntityCreateResult(bool Ok, string? Error, Guid? Id, string? EntityType, string? Name, string? Slug,
    string? Summary, JsonNode? Record, IReadOnlyList<string> Warnings)
{
    public static EntityCreateResult Fail(string error) => new(false, error, null, null, null, null, null, null, []);
}

/// <summary>
/// The generic create path (PRS-T864, plan Phase 3 "event is first-class"): an entity of a type that has
/// no repository — an <c>event</c> (a crossfire, a roundup, a berserk machine), or any custom type a
/// <c>create_repository</c> tile names — written as the universal <see cref="Entity"/> row plus its
/// <see cref="Record"/> JSON, and nothing else. Before this the only creators were the typed
/// <c>create_*</c> tools, so an event could not exist as a thing that beats tag and edges point at.
///
/// <para>Types a repository serves (and characters) are refused here: their typed tables and mappers are
/// written by their own <c>create_*</c> tools, and a bare row would read as "nothing stored". Once made,
/// the record is edited with <c>set_entity_fields</c>, whose repo-less fallback merge-patches the JSON.</para>
///
/// <para>The entity lands in the ambient universe (the DbContext stamps it), status canon, provenance
/// authored: a human asked for it by name. A slug already taken by the same type in the universe is
/// refused, never overwritten.</para>
/// </summary>
public sealed class EntityCreator(IDbContextFactory<ProseDbContext> dbFactory)
{
    static readonly Regex TypePattern = new("^[a-z][a-z0-9_]{1,39}$", RegexOptions.Compiled);

    public async Task<EntityCreateResult> CreateAsync(string type, string name, string? summary, string? recordJson,
        CancellationToken ct = default)
    {
        type = (type ?? "").Trim().ToLowerInvariant();
        name = (name ?? "").Trim();
        if (!TypePattern.IsMatch(type))
            return EntityCreateResult.Fail($"'{type}' is not an entity type: use lower_snake_case letters and digits, e.g. \"event\".");
        if (type == "character" || EntityFieldWriter.Repositories.ContainsKey(type) || CanonRecordLoader.MappedTypes.Contains(type))
            return EntityCreateResult.Fail($"a {type} has a typed record; make it with create_{type} (create_character for people), not create_entity.");
        if (type == "stub")
            return EntityCreateResult.Fail("'stub' is the placeholder type interchange uses for unresolved references; it is never created by hand.");
        if (name.Length == 0) return EntityCreateResult.Fail("name is required.");

        JsonObject record;
        if (string.IsNullOrWhiteSpace(recordJson)) record = new JsonObject();
        else
        {
            try { record = JsonNode.Parse(recordJson) as JsonObject ?? throw new System.Text.Json.JsonException(); }
            catch (System.Text.Json.JsonException) { return EntityCreateResult.Fail("record must be one JSON object: {\"field\": value, …}."); }
        }
        if (EntityFieldWriter.Refused.Keys.FirstOrDefault(k => k != "type" && record.ContainsKey(k)) is { } refusedKey)
            return EntityCreateResult.Fail($"'{refusedKey}' is not written here: {EntityFieldWriter.Refused[refusedKey]}");
        if (record.ContainsKey(EntityFieldWriter.SummaryKey))
            return EntityCreateResult.Fail($"pass the summary as its own argument, not as a record key '{EntityFieldWriter.SummaryKey}'.");
        if (record["type"] is { } t && !string.Equals(t.ToString(), type, StringComparison.OrdinalIgnoreCase))
            return EntityCreateResult.Fail($"the record's 'type' ({t}) disagrees with the entity type ({type}).");
        // The record carries its own name and type, as every stored record does, so the JSON alone says what it is.
        record["name"] = name;
        record["type"] = type;

        var slug = UniverseGraphService.Slugify(name);
        if (slug.Length == 0) return EntityCreateResult.Fail($"'{name}' has no letters or digits to make a slug from.");
        var value = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        var warnings = new List<string>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Ambient universe (query filter): the same scope the insert stamp writes into.
        if (await db.Entities.AsNoTracking().AnyAsync(e => e.EntityType == type && e.Slug == slug, ct))
        {
            var taken = await db.Entities.AsNoTracking().Where(e => e.EntityType == type && e.Slug == slug).Select(e => e.Id).FirstAsync(ct);
            return EntityCreateResult.Fail($"a {type} with slug '{slug}' already exists in this universe ({taken:N}); edit it with set_entity_fields.");
        }
        var namesakes = await db.Entities.AsNoTracking().Where(e => e.Name == name && e.EntityType != type)
            .Select(e => e.EntityType + " " + e.Id).Take(5).ToListAsync(ct);
        if (namesakes.Count > 0)
            warnings.Add($"another entity already has the name '{name}' ({string.Join(", ", namesakes)}); prose tags are by id, so both stay distinct.");

        if (!await db.RepositoryDefinitions.AnyAsync(r => r.Slug == type, ct))
        {
            var label = char.ToUpperInvariant(type[0]) + type[1..].Replace('_', ' ');
            db.RepositoryDefinitions.Add(new RepositoryDefinition
            {
                Slug = type,
                Name = label + "s",
                Category = "World",
                Icon = type == "event" ? "bi-lightning" : "bi-box",
                Description = type == "event"
                    ? "Things that happened at a place and a story time, tagged in the beats that show them (PRS-T864)."
                    : $"'{type}' entities made with create_entity.",
                RoutePath = $"/repo/{type}",
            });
            warnings.Add($"new repository '{type}' (the index tile for this type) was registered.");
        }

        var now = DateTime.UtcNow;
        var entity = new Entity
        {
            EntityType = type,
            Name = name,
            Slug = slug,
            Status = "canon",
            Description = value,
            Provenance = ClaimProvenance.Authored,
            CreatedAt = now,
            ModifiedAt = now,
        };
        db.Entities.Add(entity);
        db.Records.Add(new Record { EntityId = entity.Id, Json = record.ToJsonString(), UpdatedAt = now });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) { return EntityCreateResult.Fail($"the save was refused: {ex.InnerException?.Message ?? ex.Message}"); }
        catch (WriteGate.WriteGateRejectedException ex) { return EntityCreateResult.Fail($"the save was refused: {ex.Message}"); }

        await using var check = await dbFactory.CreateDbContextAsync(ct);
        var back = await check.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.Id == entity.Id)
            .Select(e => new { e.Name, e.Slug, e.Description, e.EntityType }).FirstOrDefaultAsync(ct);
        if (back == null) return EntityCreateResult.Fail("saved, but the entity did not read back.");
        return new EntityCreateResult(true, null, entity.Id, back.EntityType, back.Name, back.Slug, back.Description,
            CanonRecordLoader.Load(check, back.EntityType, entity.Id), warnings);
    }
}
