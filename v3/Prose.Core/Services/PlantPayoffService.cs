using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// PlantPayoffService
//
// Persists narrative "plants" (seeded details) and their payoffs per node.
// Enforces the invariant: "reward re-reading without requiring it."
//
//   GetByNodeAsync      — all registered pairs for a node
//   BuildPlantContextAsync— context block injected into BeatGeneratorService
//   RegisterAsync         — create a new plant/payoff pair
//   LinkPlantBeatAsync    — bind the plant to an actual beat after writing
//   LinkPayoffBeatAsync   — bind the payoff to an actual beat after writing
//   SetTransparencyAsync  — mark a pair transparent + note what the re-reader gains
//   AuditAsync            — find orphaned plants, opaque payoffs, and coverage gaps
// ─────────────────────────────────────────────────────────────────────────────

public class PlantPayoffService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public async Task<List<PlantPayoff>> GetByNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // SS-A43: for book-mode nodes, plants are registered on chapter children — but
        // RegisterAsync takes an arbitrary nodeId, so a plant CAN legitimately be registered
        // directly on the book itself even when it has chapters. Always include nodeId plus
        // every leaf descendant, recursing past any nested Collection (2026-08-09 fix).
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        if (!searchIds.Contains(nodeId)) searchIds.Add(nodeId);
        return await db.PlantPayoffs
            .AsNoTracking()
            .Where(p => searchIds.Contains(p.NodeId))
            .OrderBy(p => p.SortKey)
            .ToListAsync(ct);
    }

    // 2026-08-22 fix: how close to the book's end an unpaid (seeded, no payoff beat yet) plant
    // starts escalating from a neutral status line to an explicit "pay this off soon" warning.
    private const int UrgencyWindowBeats = 5;

    /// <param name="beatIndex">Zero-based position of the beat about to be written, when known.
    /// 0 (default) with <paramref name="totalBeats"/> also 0 disables the urgency escalation —
    /// matches every pre-existing caller (the coverage-telemetry call in ProseWriterRouter and
    /// any legacy direct BeatGeneratorService caller) exactly, no behavior change for them.</param>
    /// <param name="totalBeats">Total beats in the node this plant/payoff set is scoped to.</param>
    public async Task<string> BuildPlantContextAsync(Guid nodeId, int beatIndex = 0, int totalBeats = 0, CancellationToken ct = default)
    {
        var plants = await GetByNodeAsync(nodeId, ct);
        if (plants.Count == 0) return "";

        var beatsRemaining = totalBeats > 0 ? totalBeats - beatIndex : int.MaxValue;
        var unpaidCount = plants.Count(p => p.PlantBeatId != null && p.PayoffBeatId == null);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine();
        sb.AppendLine("[PLANTED DETAILS — reward re-reading without requiring it]");
        sb.AppendLine("These pairs are registered for this node. Every payoff beat must make");
        sb.AppendLine("complete sense to a cold reader; the plant makes it richer on re-read.");
        if (unpaidCount > 0 && beatsRemaining <= UrgencyWindowBeats)
            sb.AppendLine($"⚠ URGENT: {unpaidCount} seeded plant(s) below are still unpaid with only " +
                $"~{beatsRemaining} beat(s) left in this book — pay one off in THIS beat if the scene allows it.");
        foreach (var p in plants)
        {
            var status = p.PayoffBeatId != null ? "paid off"
                       : p.PlantBeatId  != null ? "seeded — payoff not yet written"
                       :                           "planned";
            var flag = !p.IsTransparent && p.PayoffBeatId != null
                ? "  ⚠ TRANSPARENCY ISSUE — payoff not yet readable without plant"
                : "";
            sb.AppendLine($"  [{p.Category.ToUpper()}] {p.PlantDescription} → {p.PayoffDescription}  ({status}){flag}");
        }
        return sb.ToString();
    }

    public async Task<PlantPayoff> RegisterAsync(
        Guid nodeId,
        string plantDesc,
        string payoffDesc,
        string category = "detail",
        Guid? plantBeatId = null,
        Guid? payoffBeatId = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FindAsync(new object[] { nodeId }, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var pp = new PlantPayoff
        {
            NodeId         = nodeId,
            UniverseId       = node.UniverseId,
            PlantDescription = plantDesc.Trim(),
            PayoffDescription= payoffDesc.Trim(),
            Category         = category.ToLowerInvariant(),
            PlantBeatId      = plantBeatId,
            PayoffBeatId     = payoffBeatId,
            SortKey          = await NextSortKeyAsync(db, nodeId, ct),
        };
        db.PlantPayoffs.Add(pp);
        await db.SaveChangesAsync(ct);
        return pp;
    }

    public async Task LinkPlantBeatAsync(Guid id, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pp = await db.PlantPayoffs.FindAsync(new object[] { id }, ct)
            ?? throw new InvalidOperationException($"PlantPayoff {id} not found.");
        pp.PlantBeatId = beatId;
        pp.UpdatedAt   = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task LinkPayoffBeatAsync(Guid id, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pp = await db.PlantPayoffs.FindAsync(new object[] { id }, ct)
            ?? throw new InvalidOperationException($"PlantPayoff {id} not found.");
        pp.PayoffBeatId = beatId;
        pp.UpdatedAt    = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetTransparencyAsync(Guid id, bool isTransparent, string? note, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pp = await db.PlantPayoffs.FindAsync(new object[] { id }, ct)
            ?? throw new InvalidOperationException($"PlantPayoff {id} not found.");
        pp.IsTransparent   = isTransparent;
        pp.TransparencyNote = note?.Trim();
        pp.UpdatedAt       = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PlantPayoffAudit> AuditAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        // SS-A43: for book-mode nodes, plants are registered on chapter children — but
        // RegisterAsync takes an arbitrary nodeId, so always include nodeId itself too.
        // Recurses past any nested Collection (2026-08-09 fix).
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        if (!searchIds.Contains(nodeId)) searchIds.Add(nodeId);
        var all = await db.PlantPayoffs
            .AsNoTracking()
            .Where(p => searchIds.Contains(p.NodeId))
            .OrderBy(p => p.SortKey)
            .ToListAsync(ct);

        var orphaned     = all.Where(p => p.PlantBeatId != null && p.PayoffBeatId == null).ToList();
        var notTransparent = all.Where(p => !p.IsTransparent && p.PayoffBeatId != null).ToList();
        // 2026-08-22 fix: docs/LOGIC.md's plant/payoff ledger dimension is documented as
        // two-way ("every plant pays, every payoff was planted") but this audit previously only
        // ever checked the first direction (Orphaned, above). A payoff beat can legitimately get
        // linked (LinkPayoffBeatAsync) before its plant beat does — this catches that reverse
        // case: a payoff written into prose with no plant beat on record yet.
        var unplanted    = all.Where(p => p.PayoffBeatId != null && p.PlantBeatId == null).ToList();
        var paidOff      = all.Count(p => p.PayoffBeatId != null);
        var planted      = all.Count(p => p.PlantBeatId  != null);

        return new PlantPayoffAudit(
            NodeSlug:           node.Slug,
            NodeTitle:          node.Title,
            TotalPairs:           all.Count,
            Planted:              planted,
            PaidOff:              paidOff,
            Orphaned:             orphaned.Count,
            Unplanted:            unplanted.Count,
            NotTransparentCount:  notTransparent.Count,
            AllPairs:             all,
            OrphanedPlants:       orphaned,
            UnplantedPayoffs:     unplanted,
            NotTransparentPayoffs: notTransparent);
    }

    static async Task<double> NextSortKeyAsync(ProseDbContext db, Guid nodeId, CancellationToken ct)
    {
        var max = await db.PlantPayoffs
            .Where(p => p.NodeId == nodeId)
            .MaxAsync(p => (double?)p.SortKey, ct);
        return (max ?? 0) + 100;
    }
}

// ── Result models ─────────────────────────────────────────────────────────────

public record PlantPayoffAudit(
    string            NodeSlug,
    string            NodeTitle,
    int               TotalPairs,
    int               Planted,
    int               PaidOff,
    int               Orphaned,
    int               Unplanted,
    int               NotTransparentCount,
    List<PlantPayoff> AllPairs,
    List<PlantPayoff> OrphanedPlants,
    List<PlantPayoff> UnplantedPayoffs,
    List<PlantPayoff> NotTransparentPayoffs);
