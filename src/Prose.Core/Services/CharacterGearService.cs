using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Surgical read/remove over <see cref="CharacterBelongingsGear"/> — a character's signature gear
/// and pharmaceuticals list — plus a corpus-wide search.
///
/// <para><b>Why this exists (2026-09-03).</b> There was no sanctioned way to remove ONE gear
/// entry. <c>create_character</c> round-trips the whole <c>CharacterData</c> through
/// <c>CharacterMapper</c>'s delete-all-and-reinsert, so correcting a single invented item meant
/// rewriting the entire record and putting every other field at risk. Found when the author ruled
/// that Kyle's "Corundum Draw Strop" is not canon: a signature-gear entry carrying a
/// 1,500-character provenance story that named a maker who does not exist. It appeared in ZERO
/// beats corpus-wide — it existed only in the record, where the generation pipeline loaded it as
/// established fact on every beat Kyle appeared in. Invented gear in a record is worse than
/// invented gear on the page, because nothing reads the record critically.</para>
///
/// <para>Shared by <c>prose --character-gear</c> and the <c>*_character_gear</c> MCP tools so the
/// two cannot drift — the same reason <see cref="Data.EntityResolver"/> was extracted after two
/// mappers kept private copies of one resolution rule.</para>
///
/// <para>The table is system-versioned: removals stay recoverable from
/// <c>CharacterBelongingsGear_History</c>.</para>
/// </summary>
public class CharacterGearService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<CharacterGearService> log;

    public CharacterGearService(IDbContextFactory<ProseDbContext> dbFactory, ILogger<CharacterGearService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    public sealed record GearRow(long Id, Guid CharacterId, string Owner, string Bucket, int Position, string GearName, Guid? GearEntityId);

    /// <summary>Resolve a character by Guid (any format) or exact name. Null when nothing matches.</summary>
    public async Task<(Guid Id, string Name)?> ResolveCharacterAsync(string who, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(who)) return null;
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (Guid.TryParse(who, out var parsed))
        {
            // IgnoreQueryFilters: an explicitly-named id must resolve regardless of ambient
            // universe, or a valid id reads as "not found" (see
            // feedback_explicit_id_lookups_need_ignorequeryfilters).
            var byId = await db.Characters.IgnoreQueryFilters().AsNoTracking()
                .Where(c => c.Id == parsed).Select(c => new { c.Id, c.Name }).FirstOrDefaultAsync(ct);
            if (byId != null) return (byId.Id, byId.Name);
        }

        var byName = await db.Characters.AsNoTracking()
            .Where(c => c.Name == who).Select(c => new { c.Id, c.Name }).FirstOrDefaultAsync(ct);
        return byName == null ? null : (byName.Id, byName.Name);
    }

    /// <summary>One character's gear entries, optionally filtered to a single bucket.</summary>
    public async Task<List<GearRow>> ListAsync(Guid characterId, string? bucket = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.CharacterBelongingsGear.AsNoTracking().Where(g => g.CharacterId == characterId);
        if (!string.IsNullOrWhiteSpace(bucket)) q = q.Where(g => g.Bucket == bucket);

        // Order and project in separate steps: EF cannot translate an OrderBy over a property of
        // a projected record (it sees the constructor call, not a column), so the sort has to
        // happen while the query still has real columns to sort on.
        var rows = await q
            .Join(db.Entities.IgnoreQueryFilters().AsNoTracking(), g => g.CharacterId, e => e.Id,
                (g, e) => new { g.Id, g.CharacterId, Owner = e.Name, g.Bucket, g.Position, g.GearName, g.GearEntityId })
            .OrderBy(x => x.Bucket).ThenBy(x => x.Position)
            .ToListAsync(ct);

        return rows.Select(x => new GearRow(x.Id, x.CharacterId, x.Owner, x.Bucket, x.Position, x.GearName, x.GearEntityId)).ToList();
    }

    /// <summary>
    /// Corpus-wide substring search over gear names — "does any character still carry X?".
    /// Deliberately mirrors <c>--entity-relationships --search</c>: a per-character-only read is
    /// how invented canon survives a purge unnoticed.
    /// </summary>
    public async Task<List<GearRow>> SearchAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return new();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pattern = $"%{text.Trim()}%";

        var rows = await db.CharacterBelongingsGear.AsNoTracking()
            .Where(g => EF.Functions.Like(g.GearName, pattern))
            .Join(db.Entities.AsNoTracking(), g => g.CharacterId, e => e.Id,
                (g, e) => new { g.Id, g.CharacterId, Owner = e.Name, g.Bucket, g.Position, g.GearName, g.GearEntityId })
            .OrderBy(x => x.Owner).ThenBy(x => x.Id)
            .ToListAsync(ct);

        return rows.Select(x => new GearRow(x.Id, x.CharacterId, x.Owner, x.Bucket, x.Position, x.GearName, x.GearEntityId)).ToList();
    }

    /// <summary>
    /// Remove one gear row, scoped to its owner so a mistyped id cannot touch another character.
    /// Returns the removed row, or null when nothing matched.
    ///
    /// <para>Refreshes <c>CharacterReadModels</c> afterwards, and that is NOT optional: every read
    /// surface (<c>get_character</c> included) serves from that projection, not from this bridge
    /// table, so a surgical write without the refresh really does delete the row while every
    /// reader keeps showing it — found live during the relationship-deletion work.</para>
    /// </summary>
    public async Task<GearRow?> RemoveAsync(Guid characterId, long rowId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.CharacterBelongingsGear
            .FirstOrDefaultAsync(g => g.Id == rowId && g.CharacterId == characterId, ct);
        if (row == null) return null;

        var owner = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.Id == characterId).Select(e => e.Name).FirstOrDefaultAsync(ct) ?? "";
        var snapshot = new GearRow(row.Id, row.CharacterId, owner, row.Bucket, row.Position, row.GearName, row.GearEntityId);

        db.CharacterBelongingsGear.Remove(row);
        await db.SaveChangesAsync(ct);

        try { await CharacterMapper.RefreshReadModelAsync(db, characterId, ct); }
        catch (Exception ex)
        {
            log.LogWarning(ex,
                "Gear row {RowId} removed from character {CharacterId} but the read-model refresh failed — " +
                "readers may serve the old value until the projection is rebuilt", rowId, characterId);
        }

        log.LogInformation("Removed gear row {RowId} ({Bucket}) from character {CharacterId}", rowId, snapshot.Bucket, characterId);
        return snapshot;
    }
}
