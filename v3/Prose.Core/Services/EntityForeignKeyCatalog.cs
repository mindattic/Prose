using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Every real SQL Server foreign key targeting <c>Entities.Id</c>, discovered from
/// <c>sys.foreign_keys</c>/<c>sys.foreign_key_columns</c> metadata — not a <c>%EntityId%</c>
/// name-pattern guess. The naming-convention scan this replaced
/// (<see cref="DuplicateEntityScanService"/>'s old <c>DiscoverEntityForeignKeysAsync</c>) missed
/// real constraints whose FK column isn't literally named "...EntityId"
/// (<c>CharacterHomeTurf.PlaceId</c>, <c>BookProtagonist.CharacterId</c>,
/// <c>ChapterCharacter.CharacterId</c>) — harmless under the old soft-disable design (the loser
/// row never actually vanished, so the FK never fired), but exactly the gap that would let a real
/// hard-delete throw a live FK violation mid-transaction. Two consumers: merge relink
/// (<see cref="DuplicateEntityScanService.MergeAsync"/>, every FK regardless of cascade) and the
/// plain-delete safety gate (<see cref="EntityDeleteGuard"/>, non-cascading FKs only).
///
/// <see cref="EntityFk.IsCompositeKey"/> — found live 2026-08-17: the FIRST version of this
/// discovery silently EXCLUDED any table whose primary key spans more than one column
/// (<c>BeatEntityMentions</c> (BeatId, EntityId), <c>EntityTags</c> (EntityId, TagId),
/// <c>EntityTaxonomies</c> (EntityId, TaxonomyId, StoryValidFrom) — all three CASCADE-linked to
/// Entities). That meant <c>MergeAsync</c> never even attempted to preserve those rows before the
/// CASCADE fired on the loser's Entities delete — 5 real BeatEntityMentions rows were silently,
/// untracked-ly lost during the first real M-101 merge this way. Now included (tagged
/// <c>IsCompositeKey=true</c>) so <c>MergeAsync</c> can route them to the safe capture-and-delete
/// path instead of the update-based relink — an UPDATE-based relink's undo entry needs a single
/// column that uniquely identifies the row for later point-in-time reversal, which no single
/// column of a composite key still provides once other rows already share the winner's id.
/// </summary>
public static class EntityForeignKeyCatalog
{
    public sealed record EntityFk(string Table, string Column, string PkColumn, bool Cascades, bool IsCompositeKey);

    public static async Task<List<EntityFk>> DiscoverAsync(ProseDbContext db, CancellationToken ct = default)
    {
        // sys.foreign_keys is SQL-Server-only. No-op on other providers (SQLite unit tests) —
        // same convention as ProseDbContext.EnableSystemVersioningAsync. A caller (EntityDeleteGuard)
        // that gets back an empty list simply finds no blockers, which is the correct behavior for
        // a test double that has no real FK metadata to check in the first place.
        if (!db.Database.IsSqlServer()) return [];

        var fkRows = await db.Database.SqlQueryRaw<FkRow>("""
            SELECT tp.name AS TableName, cp.name AS ColumnName, CAST(fk.delete_referential_action AS int) AS DeleteAction
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            JOIN sys.tables tp ON tp.object_id = fkc.parent_object_id
            JOIN sys.columns cp ON cp.object_id = tp.object_id AND cp.column_id = fkc.parent_column_id
            JOIN sys.tables tr ON tr.object_id = fk.referenced_object_id
            WHERE tr.name = 'Entities'
              AND tp.name NOT LIKE '%\_History' ESCAPE '\'
            """).ToListAsync(ct);

        if (fkRows.Count == 0) return [];

        var pkRows = await db.Database.SqlQueryRaw<PkColumnRow>("""
            SELECT t.name AS TableName, ic.name AS PkColumn, CAST(icx.key_ordinal AS int) AS KeyOrdinal
            FROM sys.indexes i
            JOIN sys.tables t ON t.object_id = i.object_id
            JOIN sys.index_columns icx ON icx.object_id = i.object_id AND icx.index_id = i.index_id
            JOIN sys.columns ic ON ic.object_id = icx.object_id AND ic.column_id = icx.column_id
            WHERE i.is_primary_key = 1
            """).ToListAsync(ct);

        // One representative column per table (lowest key_ordinal) plus whether the PK spans more
        // than one column. The representative is used only for RowMutationUndo's PkColumn/PkValue
        // bookkeeping fields on the "delete" op — never for an "update" op's reversal WHERE clause
        // (composite-key tables are routed away from the update/relink path entirely; see below).
        var pkByTable = pkRows.GroupBy(r => r.TableName)
            .ToDictionary(g => g.Key, g => (
                Representative: g.OrderBy(r => r.KeyOrdinal).First().PkColumn,
                IsComposite: g.Count() > 1));

        return fkRows
            .Where(r => pkByTable.ContainsKey(r.TableName))
            // delete_referential_action: 0=NO_ACTION (maps to Restrict/NoAction in EF terms),
            // 1=CASCADE, 2=SET_NULL, 3=SET_DEFAULT. Only true CASCADE means "this row disappears
            // on its own when the referenced Entity is deleted" — everything else still exists
            // afterward and either blocks the delete (NO_ACTION) or needs handling (SET_NULL/
            // SET_DEFAULT, none configured on this schema today, treated conservatively as
            // non-cascading so EntityDeleteGuard still flags them rather than assuming safety).
            .Select(r => new EntityFk(r.TableName, r.ColumnName, pkByTable[r.TableName].Representative,
                r.DeleteAction == 1, pkByTable[r.TableName].IsComposite))
            .ToList();
    }

    private sealed class FkRow
    {
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public int DeleteAction { get; set; }
    }

    private sealed class PkColumnRow
    {
        public string TableName { get; set; } = "";
        public string PkColumn { get; set; } = "";
        public int KeyOrdinal { get; set; }
    }
}
