using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Inserts a minimal <see cref="Entity"/> row (+ the matching subtype row) — just enough to be a
/// valid foreign-key target. Extracted from <c>FactInterpreterService</c> (2026-09-15) so the
/// narrative-obligation extractor can create stubs for unnamed referents from the beat-write path;
/// until then no stub was ever created from prose, which is exactly how "the girl behind the
/// curtain" existed for 475 beats without a record.
///
/// <para>An unnamed referent's stub is named <c>"(unnamed) girl behind the curtain"</c>. The
/// leading <c>(</c> is load-bearing: <c>EntityMentionScanner.BuildCandidateIndexAsync</c> skips
/// names starting with <c>(</c>, so a lowercase descriptive phrase is never auto-tagged across the
/// corpus. The author names her — or drops her — from the obligations board.</para>
/// </summary>
public static class EntityStubFactory
{
    public const string UnnamedPrefix = "(unnamed) ";

    public static async Task<Guid> CreateStubAsync(
        ProseDbContext db,
        Guid universeId,
        string name,
        string entityType,
        string? description,
        Guid? originNodeId = null,
        string provenance = ClaimProvenance.Scaffolded,
        CancellationToken ct = default)
    {
        var id = Guid.CreateVersion7();
        var slug = UniverseGraphService.Slugify(name);
        // Disambiguate against any stale matching slug
        if (await db.Entities.IgnoreQueryFilters().AnyAsync(e => e.EntityType == entityType && e.Slug == slug, ct))
            slug = $"{slug}-{id:N}";

        db.Entities.Add(new Entity
        {
            Id           = id,
            UniverseId   = universeId,
            EntityType   = entityType,
            Name         = name,
            Slug         = slug,
            Status       = "stub",
            Description  = description,
            OriginNodeId = originNodeId,
            Provenance   = provenance,
            CreatedAt    = DateTime.UtcNow,
            ModifiedAt   = DateTime.UtcNow,
        });

        // Add a matching subtype row when one is known. Each subtype has its own PK = Id; we just
        // need Id + Name — the rest is backfilled by the canonical importer or a UI edit.
        switch (entityType)
        {
            case "character":      db.Characters     .Add(new Character     { Id = id, Name = name }); break;
            case "place":          db.Places         .Add(new Place         { Id = id, Name = name }); break;
            case "faction":        db.Factions       .Add(new Faction       { Id = id, Name = name }); break;
            case "corponation":    db.Corponations   .Add(new Corponation   { Id = id, Name = name }); break;
            case "subsidiary":     db.Subsidiaries   .Add(new Subsidiary    { Id = id, Name = name }); break;
            case "automaton":      db.Automata       .Add(new Automaton     { Id = id, Name = name }); break;
            case "weapon":         db.Weapons        .Add(new Weapon        { Id = id, Name = name }); break;
            case "equipment":      db.EquipmentItems .Add(new Equipment     { Id = id, Name = name }); break;
            case "cyberware":      db.CyberwareItems .Add(new Cyberware     { Id = id, Name = name }); break;
            case "apparel":        db.Apparels       .Add(new Apparel       { Id = id, Name = name }); break;
            case "ammunition":     db.Ammunitions    .Add(new Ammunition    { Id = id, Name = name }); break;
            // Other types either lack a strict subtype table or use generic Entity rows directly.
        }
        await db.SaveChangesAsync(ct);
        return id;
    }

    /// <summary>An unnamed referent's stub: <c>"(unnamed) …"</c>, described only by the verbatim
    /// sentence that introduced it — nothing invented. Reuses an existing stub with the same
    /// label in the same book rather than minting a second.</summary>
    public static async Task<Guid> GetOrCreateUnnamedAsync(
        ProseDbContext db, Guid universeId, Guid bookNodeId, string label, string entityType, string quote,
        CancellationToken ct = default)
    {
        var name = UnnamedPrefix + QuoteGroundingLabel(label);
        var existing = await db.Entities.IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId && e.OriginNodeId == bookNodeId && e.Name == name && e.Status != "archived")
            .Select(e => e.Id)
            .FirstOrDefaultAsync(ct);
        if (existing != Guid.Empty) return existing;

        return await CreateStubAsync(db, universeId, name, entityType, quote, bookNodeId, ClaimProvenance.Observed, ct);
    }

    private static string QuoteGroundingLabel(string label)
    {
        var l = Audit.QuoteGrounding.Normalize(label).ToLowerInvariant();
        foreach (var article in new[] { "the ", "a ", "an " })
            if (l.StartsWith(article, StringComparison.Ordinal)) { l = l[article.Length..]; break; }
        return l.Length > 80 ? l[..80] : l;
    }
}
