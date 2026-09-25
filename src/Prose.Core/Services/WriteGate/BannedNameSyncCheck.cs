using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.WriteGate;

/// <summary>
/// Rejects an <see cref="Entity"/> Name (character/place/faction/corponation/weapon/…) or a
/// CharacterAlias/PlaceAlias/FactionAlias/WeaponAlias Value outright when it contains a
/// registered <see cref="BannedName"/> as a whole word, case-insensitive — across every
/// universe, with no canonical replacement offered (unlike <see cref="DeprecatedEntityName"/>,
/// which is a per-universe rename map scanned only after the fact by prose sweeps). Covers every
/// alias table in the schema (all ~20 <c>*Alias</c> DbSets), not just the Character/Place/
/// Faction/Weapon ones — a gap found and closed 2026-08-28. Same
/// "make the invariant structurally impossible at the one chokepoint every write passes
/// through" reasoning as <see cref="SelfAliasSyncCheck"/>.
///
/// Forward-only by author ruling (2026-08-26): this check only fires on a name the row did not
/// already carry — a new row, a renamed entity, a new alias — so a name banned today does not
/// retroactively invalidate anything already in the database, and does not block edits to it.
/// </summary>
public sealed class BannedNameSyncCheck : IWriteGateSyncCheck
{
    public bool AppliesTo(EntityEntry entry) =>
        (entry.State == EntityState.Added || entry.State == EntityState.Modified)
        && entry.Entity is Entity or CharacterAlias or PlaceAlias or FactionAlias or WeaponAlias
            or AutomatonAlias or EquipmentAlias or CyberwareItemAlias or ApparelAlias or AmmunitionAlias
            or PharmAlias or GenemodAlias or MaterialAlias or TransportationAlias or ConsumerGoodAlias
            or LabSpecimenAlias or PsionicAlias or TechnologyAlias or EntertainmentAlias
            or FlyoverEntityAlias or SyntheticLifeAlias;

    public async Task CheckAsync(EntityEntry entry, CancellationToken ct)
    {
        var value = ValueOf(entry.Entity);
        if (string.IsNullOrWhiteSpace(value)) return;

        // Forward-only means a name the row did not already carry. A modified Entities row whose
        // Name did not change is not a new name: every save bumps ModifiedAt, so without this the
        // one character the author let keep a banned name (Dr. Nadia Park) could never be edited
        // at all — found live 2026-09-23 correcting her record. Likewise an alias row the mapper
        // deletes and re-inserts with the same value in the same save is not a new alias.
        if (entry.State == EntityState.Modified && entry.Entity is Entity)
        {
            var name = entry.Property(nameof(Entity.Name));
            if (!name.IsModified || string.Equals(name.OriginalValue as string, value, StringComparison.OrdinalIgnoreCase)) return;
        }
        if (entry.State == EntityState.Added && entry.Entity is not Entity
            && entry.Context.ChangeTracker.Entries().Any(e => e.State == EntityState.Deleted
                && e.Entity.GetType() == entry.Entity.GetType()
                && string.Equals(ValueOf(e.Entity), value, StringComparison.OrdinalIgnoreCase)))
            return;
        // Mappers delete aliases with ExecuteDelete (invisible to the tracker) and re-add them:
        // they note the old values on the context so an unchanged alias is not "new".
        if (entry.State == EntityState.Added && entry.Entity is not Entity
            && entry.Context is ProseDbContext pdb && pdb.WasReplacedInPendingSave(entry.Entity.GetType(), value))
            return;

        var db = (ProseDbContext)entry.Context;

        // The list is small and rarely written to — re-query every time rather than caching,
        // since a cache would go stale the instant someone adds a ban mid-session.
        var banned = await db.BannedNames.AsNoTracking().Select(b => b.Name).ToListAsync(ct);
        if (banned.Count == 0) return;

        foreach (var name in banned)
        {
            if (Regex.IsMatch(value, $@"\b{Regex.Escape(name)}\b", RegexOptions.IgnoreCase))
                throw new WriteGateRejectedException(
                    $"Rejected: \"{value}\" contains the Prose-wide banned name \"{name}\" — " +
                    "banned across every universe (forward-only; pre-existing rows using it are " +
                    "unaffected). Choose a different name.");
        }
    }

    private static string? ValueOf(object entity) =>
        entity switch
        {
            Entity ent => ent.Name,
            CharacterAlias ca => ca.Value,
            PlaceAlias pa => pa.Value,
            FactionAlias fa => fa.Value,
            WeaponAlias wa => wa.Value,
            AutomatonAlias aa => aa.Value,
            EquipmentAlias eqa => eqa.Value,
            CyberwareItemAlias cwa => cwa.Value,
            ApparelAlias apa => apa.Value,
            AmmunitionAlias ama => ama.Value,
            PharmAlias pha => pha.Value,
            GenemodAlias gma => gma.Value,
            MaterialAlias mta => mta.Value,
            TransportationAlias tra => tra.Value,
            ConsumerGoodAlias cga => cga.Value,
            LabSpecimenAlias lsa => lsa.Value,
            PsionicAlias psa => psa.Value,
            TechnologyAlias tea => tea.Value,
            EntertainmentAlias ena => ena.Value,
            FlyoverEntityAlias fea => fea.Value,
            SyntheticLifeAlias sla => sla.Value,
            _ => null,
        };
}
