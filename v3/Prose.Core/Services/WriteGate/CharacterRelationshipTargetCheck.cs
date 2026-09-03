using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.WriteGate;

/// <summary>
/// Guards <c>CharacterRelationships</c> — the table <see cref="WriteSubject.CharacterRelationship"/>
/// was declared for and deliberately left unrouted "until a concrete problem surfaces."
///
/// <para><b>The concrete problem (2026-09-02).</b> Seven relationship rows describing BCODA's Kyle
/// were written onto <i>Seo Jisun</i> — a real, unrelated character from a different book
/// (Testament) — by <c>CanonGroundingService</c>'s auto-scaffolder, whose parser split an LLM
/// claim on <c>" of "</c> and, when the claim contained no such connector, wrote a row with an
/// EMPTY <c>TargetName</c> and the raw sentence duplicated into Type and Description ("gave Kyle
/// his katana", "his funeral"). That fingerprint — a relationship pointing at nothing, described
/// by a sentence — is what this check makes impossible to store. The parser itself was fixed in
/// Phase 0; this is the backstop for every future writer, since the parser was not the only one
/// possible and <c>audit_data_consistency</c> contained zero references to this table.</para>
///
/// <para><b>Two rules, and deliberately not a third.</b></para>
/// <list type="number">
/// <item><b>Empty target — rejected.</b> A relationship to nobody is not a fact about anyone. This
/// is the exact shape of the contamination.</item>
/// <item><b>Cross-book target — rejected</b> when the source character and the resolved target are
/// each scoped to a DIFFERENT book (both <see cref="Entity.OriginNodeId"/> non-null and unequal).
/// <see cref="CrossUniverseOriginCheck"/> guards the cross-<i>universe</i> case; this contamination
/// was cross-<i>book within one universe</i>, which nothing guarded. A null origin on either side
/// means "universe-wide, shared by every book" (GLMZ's Kyle is exactly that) and is allowed — it
/// is the designed, common case, not a missing scope.</item>
/// </list>
///
/// <para><b>Not a rule: an unresolvable target.</b> The Phase 3 spec proposed rejecting a
/// <c>TargetName</c> that resolves to no entity. That is deliberately NOT enforced here, and the
/// deviation is stated rather than quietly taken: CLAUDE.md's own Stage 2 gate permits a
/// relationship that is "explicitly an intentional off-page reference", the table carries no field
/// distinguishing that from an unseeded target, and <c>CharacterMapper.PersistAsync</c> reinserts
/// EVERY row on every character Save — so rejecting unresolved targets would make an unrelated
/// edit to any character carrying a legacy unresolved row fail outright. Unresolved rows are
/// reported instead, by <c>--entity-relationships --orphans</c> and by
/// <c>audit_data_consistency</c>'s CHAR-REL-UNRESOLVED check. A gate that blocks legitimate
/// authoring gets disabled; a report gets read.</para>
/// </summary>
public sealed class CharacterRelationshipTargetCheck : IWriteGateSyncCheck
{
    public bool AppliesTo(EntityEntry entry) =>
        (entry.State == EntityState.Added || entry.State == EntityState.Modified)
        && entry.Entity is CharacterRelationshipRow;

    public async Task CheckAsync(EntityEntry entry, CancellationToken ct)
    {
        var row = (CharacterRelationshipRow)entry.Entity;
        var db = (ProseDbContext)entry.Context;

        if (string.IsNullOrWhiteSpace(row.TargetName))
            throw new WriteGateRejectedException(
                $"Rejected: relationship row on character {row.CharacterId} has an empty TargetName " +
                $"(type='{row.Type}', description='{Truncate(row.Description)}'). A relationship must " +
                "name what it points at — an empty target with the raw claim duplicated into Type and " +
                "Description is the fingerprint of the 2026-09-02 cross-book contamination. If the " +
                "claim is real but unparsed, file it as a finding; do not store it as canon.");

        if (row.TargetEntityId is not { } targetId) return; // unresolved: reported, not rejected (see class docs)

        var sourceOrigin = await OriginOfAsync(db, row.CharacterId, ct);
        var targetOrigin = await OriginOfAsync(db, targetId, ct);

        // Both book-scoped and to DIFFERENT books is the only unambiguous violation. A null on
        // either side is a universe-wide entity, which every book in the universe may reference.
        if (sourceOrigin is { } src && targetOrigin is { } tgt && src != tgt)
            throw new WriteGateRejectedException(
                $"Rejected: character {row.CharacterId} (scoped to book {src}) cannot hold a " +
                $"relationship to entity {targetId} '{row.TargetName}' (scoped to book {tgt}) — the two " +
                "belong to different books' continuities. This is the cross-book contamination class " +
                "(2026-08-22 OriginNodeId incident, recurred 2026-09-02 on Seo Jisun). If the target is " +
                "meant to be shared across books, clear its OriginNodeId; if this is a different " +
                "same-named entity, point the row at the right one.");
    }

    /// <summary>
    /// The entity's book scope, preferring the in-flight <c>ChangeTracker</c> value over the
    /// database — the source character or the target may be part of this very SaveChanges batch
    /// and not yet flushed, the same reason <see cref="CrossUniverseOriginCheck"/> looks there
    /// first. <c>IgnoreQueryFilters</c> because this is an explicit-id lookup: a universe filter
    /// turning a valid id into "not found" would silently skip the check.
    /// </summary>
    private static async Task<Guid?> OriginOfAsync(ProseDbContext db, Guid entityId, CancellationToken ct)
    {
        var tracked = db.ChangeTracker.Entries<Entity>().FirstOrDefault(e => e.Entity.Id == entityId);
        if (tracked != null) return tracked.Entity.OriginNodeId;

        return await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.Id == entityId).Select(e => e.OriginNodeId).FirstOrDefaultAsync(ct);
    }

    private static string Truncate(string? s) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= 80 ? s : s[..77] + "...";
}
