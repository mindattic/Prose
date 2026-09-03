using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.WriteGate;

/// <summary>
/// Refuses to create new canon (an <see cref="Entity"/> or a <see cref="Node"/>) under a universe
/// the caller never named — closing the "universe scoping is convention, not enforcement" gap
/// (Story Ledger Phase 3; author ruling 2026-09-03: <i>fail closed on writes only</i>).
///
/// <para><b>What was actually broken, verified rather than assumed.</b> The Phase 3 spec framed
/// this as <c>ScopedUniverseId == Guid.Empty</c> making the query filter a no-op across every
/// universe. That case is real but nearly unreachable in production: <c>UniverseContext</c>
/// resolves to the persisted <c>current_universe</c> default when nothing else is set, so Empty
/// only happens with no universes configured at all (tests, a fresh DB). The CLI is already fail
/// closed at its own gate — <c>Program.cs</c> refuses any non-allowlisted command with no
/// <c>--universe</c>/<c>PROSE_UNIVERSE</c>. <b>The live hole is MCP:</b> <c>HubInvoker</c> sends
/// no universe and <c>ToolDispatch</c> scopes none, so every MCP tool write executes in the Hub
/// under whatever that process's ambient default happens to be — and <c>switch_universe</c> sets
/// the MCP process's field, which the Hub process never sees unless the tool's Impl runs there.
/// So an MCP <c>create_character</c> could quietly seed a SCRY character into GLMZ, and nothing
/// would say so.</para>
///
/// <para><b>Why only ambient-stamped inserts.</b> Rejecting every write under an inherited scope
/// would break the genuinely cross-universe commands (<c>--merge-entity</c>, <c>--archive-book</c>,
/// <c>--tag-entities</c>, <c>--restore-entity</c>, the interchange importer) that resolve each
/// row's own universe by explicit id — which is correct behaviour, not a defect. Those either
/// MODIFY existing rows (whose UniverseId is their own, never stamped) or set
/// <c>UniverseId</c> themselves before Add. Only a row whose universe was supplied by the ambient
/// default — see <c>ProseDbContext.WasUniverseStampedFromAmbient</c> — is a caller that did not
/// know, or did not say, where its new canon was going. That is exactly the population worth
/// refusing, and refusing it costs nothing to anyone who names their universe.</para>
///
/// <para>Reads are untouched by design: a read under a defaulted scope returns the wrong rows and
/// the caller sees that; a write under a defaulted scope leaves wrong rows behind forever.</para>
/// </summary>
public sealed class UnscopedUniverseWriteCheck : IWriteGateSyncCheck
{
    public bool AppliesTo(EntityEntry entry) =>
        entry.State == EntityState.Added && entry.Entity is Entity or Node;

    public Task CheckAsync(EntityEntry entry, CancellationToken ct)
    {
        // Scoping inactive entirely (no universes configured — tests, design-time, pre-migration):
        // there is no "wrong universe" to land in, so there is nothing to enforce.
        if (UniverseScope.EffectiveId == Guid.Empty) return Task.CompletedTask;
        if (UniverseScope.IsExplicitlyScoped) return Task.CompletedTask;

        var db = (ProseDbContext)entry.Context;
        if (!db.WasUniverseStampedFromAmbient(entry.Entity)) return Task.CompletedTask;

        var (kind, name) = entry.Entity switch
        {
            Entity e => ($"{e.EntityType} entity", e.Name),
            Node n => ($"{n.Kind} node", n.Title),
            _ => ("row", ""),
        };

        throw new WriteGateRejectedException(
            $"Rejected: refusing to create {kind} '{name}' in universe " +
            $"{UniverseScope.Current?.CurrentSlug ?? UniverseScope.EffectiveId.ToString()} — that universe was " +
            "inherited from the persisted default, not chosen by this caller. New canon must name its " +
            "universe (SS-LAW-15, universe division is absolute). Pass --universe <slug> on the CLI, set " +
            "PROSE_UNIVERSE, call switch_universe before the write from MCP, or set UniverseId explicitly " +
            "on the row if this code already knows which universe it targets.");
    }
}
