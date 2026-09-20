using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.WriteGate;

/// <summary>
/// The write-gate's first real check (2026-08-22): rejects a <c>CharacterAlias</c>/
/// <c>PlaceAlias</c>/<c>FactionAlias</c>/<c>WeaponAlias</c> insert/update outright when its
/// <c>Value</c> matches (case-insensitively) its own owning entity's canonical <c>Name</c> — a
/// redundant, meaningless self-alias.
///
/// This is the exact bug class <c>FixSelfAliasesCli.cs</c> was built to clean up retroactively
/// and <see cref="DuplicateEntityScanService.MergeAsync"/> was patched to stop creating going
/// forward (both 2026-08-18/22) — but both of those are reactive: they clean up or prevent it in
/// ONE specific write path (a merge). This check makes it structurally impossible from ANY write
/// path, which is the actual point of the write-gate: a fast, deterministic rule enforced at the
/// one chokepoint every write already passes through (<c>ProseDbContext.SaveChanges</c>), not
/// something every future caller has to remember to check for itself.
/// </summary>
public sealed class SelfAliasSyncCheck : IWriteGateSyncCheck
{
    public bool AppliesTo(EntityEntry entry) =>
        (entry.State == EntityState.Added || entry.State == EntityState.Modified)
        && entry.Entity is CharacterAlias or PlaceAlias or FactionAlias or WeaponAlias;

    public async Task CheckAsync(EntityEntry entry, CancellationToken ct)
    {
        var (ownerId, value) = entry.Entity switch
        {
            CharacterAlias ca => (ca.CharacterId, ca.Value),
            PlaceAlias pa => (pa.PlaceId, pa.Value),
            FactionAlias fa => (fa.FactionId, fa.Value),
            WeaponAlias wa => (wa.WeaponId, wa.Value),
            _ => (Guid.Empty, (string?)null),
        };
        if (ownerId == Guid.Empty || string.IsNullOrWhiteSpace(value)) return;

        var db = (ProseDbContext)entry.Context;

        // The owning Entity row may be part of THIS SAME SaveChanges batch (a new character and
        // its alias inserted together) and therefore not yet in the database — check the
        // ChangeTracker first, since a plain LINQ query against the DB would miss it entirely
        // (this check runs before base.SaveChanges, so nothing has been flushed yet).
        var ownerName = db.ChangeTracker.Entries<Entity>()
            .FirstOrDefault(e => e.Entity.Id == ownerId)?.Entity.Name;

        ownerName ??= await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.Id == ownerId).Select(e => e.Name).FirstOrDefaultAsync(ct);

        if (ownerName != null && string.Equals(value.Trim(), ownerName.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new WriteGateRejectedException(
                $"Rejected: alias \"{value}\" equals its own entity's canonical name \"{ownerName}\" " +
                $"(self-alias, entity {ownerId}). An alias must be a genuinely different name a reader " +
                "or the text might use — not a restatement of the entity's own Name.");
    }
}
