using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

// ── NodeSpineService ─────────────────────────────────────────────────────
// Manages the per-node narrative spine: the bible (already on Node),
// user stories (new column), amendments (append-only log), and version pins
// (bridge: NodeVersion ↔ spine hashes).
//
// Invariants:
//   - AmendmentSeqNo is 1-based, monotonically increasing per node.
//   - PinVersionAsync creates exactly one pin per (NodeId, NodeVersion).
//   - ScaffoldAsync is idempotent — safe to call on every CreateNode.
// ──────────────────────────────────────────────────────────────────────────

public class NodeSpineService
{
    public record SpineDto(
        Guid         NodeId,
        string?      Bible,
        DateTime?    BibleUpdatedAt,
        string?      UserStories,
        DateTime?    UserStoriesUpdatedAt,
        List<NodeAmendment>     Amendments,
        NodeSpineVersion?       LatestPin);

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public NodeSpineService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    // ── Scaffold (called by CreateNode) ─────────────────────────────────

    public async Task ScaffoldAsync(Guid nodeId, string title, bool bibleAlreadySet, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(x => x.Id == nodeId, ct);
        if (node == null) return;

        bool changed = false;

        if (!bibleAlreadySet && string.IsNullOrWhiteSpace(node.NodeBible))
        {
            node.NodeBible = BibleTemplate(title);
            node.NodeBibleGeneratedAt = DateTime.UtcNow;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(node.NodeUserStories))
        {
            node.NodeUserStories = UserStoriesTemplate(title);
            node.NodeUserStoriesUpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        if (changed)
        {
            node.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Get full spine ─────────────────────────────────────────────────────

    public async Task<SpineDto?> GetSpineAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(x => x.Id == nodeId, ct);
        if (node == null) return null;

        var amendments = await db.NodeAmendments
            .Where(x => x.NodeId == nodeId)
            .OrderBy(x => x.SequenceNo)
            .ToListAsync(ct);

        var latestPin = await db.NodeSpineVersions
            .Where(x => x.NodeId == nodeId)
            .OrderByDescending(x => x.NodeVersion)
            .FirstOrDefaultAsync(ct);

        return new SpineDto(
            nodeId,
            node.NodeBible,
            node.NodeBibleGeneratedAt,
            node.NodeUserStories,
            node.NodeUserStoriesUpdatedAt,
            amendments,
            latestPin);
    }

    // ── Update user stories ────────────────────────────────────────────────

    public async Task SetUserStoriesAsync(Guid nodeId, string content, string updatedBy, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(x => x.Id == nodeId, ct);
        if (node == null) throw new InvalidOperationException($"Node {nodeId} not found.");

        node.NodeUserStories = content;
        node.NodeUserStoriesUpdatedAt = DateTime.UtcNow;
        node.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Append amendment ──────────────────────────────────────────────────

    public async Task<NodeAmendment> AppendAmendmentAsync(
        Guid nodeId, string summary, string body, string createdBy, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        var maxSeq = await db.NodeAmendments
            .Where(x => x.NodeId == nodeId)
            .Select(x => (int?)x.SequenceNo)
            .MaxAsync(ct) ?? 0;

        var seq  = maxSeq + 1;
        var code = $"SA-{seq}";

        var row = new NodeAmendment
        {
            Id         = Guid.NewGuid(),
            NodeId   = nodeId,
            SequenceNo = seq,
            Code       = code,
            Summary    = summary,
            Body       = body,
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = createdBy,
        };

        db.NodeAmendments.Add(row);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return row;
    }

    // ── List amendments ───────────────────────────────────────────────────

    public async Task<List<NodeAmendment>> ListAmendmentsAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.NodeAmendments
            .Where(x => x.NodeId == nodeId)
            .OrderBy(x => x.SequenceNo)
            .ToListAsync(ct);
    }

    // ── Pin spine version ─────────────────────────────────────────────────

    public async Task<NodeSpineVersion> PinVersionAsync(
        Guid nodeId, string notes, string pinnedBy, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(x => x.Id == nodeId, ct);
        if (node == null) throw new InvalidOperationException($"Node {nodeId} not found.");

        var amendmentCount = await db.NodeAmendments
            .Where(x => x.NodeId == nodeId)
            .Select(x => (int?)x.SequenceNo)
            .MaxAsync(ct) ?? 0;

        var bibleHash       = Hash(node.NodeBible ?? "");
        var userStoriesHash = Hash(node.NodeUserStories ?? "");

        // Upsert: if a pin already exists for this version, update it.
        var existing = await db.NodeSpineVersions
            .FirstOrDefaultAsync(x => x.NodeId == nodeId && x.NodeVersion == node.Version, ct);

        if (existing != null)
        {
            existing.BibleHash       = bibleHash;
            existing.UserStoriesHash = userStoriesHash;
            existing.AmendmentCount  = amendmentCount;
            existing.PinnedAt        = DateTime.UtcNow;
            existing.PinnedBy        = pinnedBy;
            existing.Notes           = notes;
            await db.SaveChangesAsync(ct);
            return existing;
        }

        var pin = new NodeSpineVersion
        {
            Id               = Guid.NewGuid(),
            NodeId         = nodeId,
            NodeVersion    = node.Version,
            BibleHash        = bibleHash,
            UserStoriesHash  = userStoriesHash,
            AmendmentCount   = amendmentCount,
            PinnedAt         = DateTime.UtcNow,
            PinnedBy         = pinnedBy,
            Notes            = notes,
        };
        db.NodeSpineVersions.Add(pin);
        await db.SaveChangesAsync(ct);
        return pin;
    }

    // ── List all pins ─────────────────────────────────────────────────────

    public async Task<List<NodeSpineVersion>> ListPinsAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.NodeSpineVersions
            .Where(x => x.NodeId == nodeId)
            .OrderByDescending(x => x.NodeVersion)
            .ToListAsync(ct);
    }

    // ── Drift check ───────────────────────────────────────────────────────
    // Returns true when the current bible or user_stories hash differs from
    // the most recent pin — meaning prose was written against stale spine docs.

    public async Task<(bool drifted, string reason)> CheckDriftAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(x => x.Id == nodeId, ct);
        if (node == null) return (false, "node not found");

        var latestPin = await db.NodeSpineVersions
            .Where(x => x.NodeId == nodeId)
            .OrderByDescending(x => x.NodeVersion)
            .FirstOrDefaultAsync(ct);

        if (latestPin == null) return (false, "no pin yet");

        var bibleHash       = Hash(node.NodeBible ?? "");
        var userStoriesHash = Hash(node.NodeUserStories ?? "");

        var reasons = new List<string>();
        if (latestPin.BibleHash != bibleHash)       reasons.Add("bible changed since last pin");
        if (latestPin.UserStoriesHash != userStoriesHash) reasons.Add("user_stories changed since last pin");

        var amendmentCount = await db.NodeAmendments
            .Where(x => x.NodeId == nodeId)
            .CountAsync(ct);
        if (amendmentCount > latestPin.AmendmentCount)
            reasons.Add($"{amendmentCount - latestPin.AmendmentCount} new amendment(s) since last pin");

        return (reasons.Count > 0, string.Join("; ", reasons));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string Hash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BibleTemplate(string title) => $"""
        # {title} — Node Bible

        ## Premise


        ## Arc


        ## Voice


        ## Characters


        ## Beat Spine


        ## Rules

        """;

    private static string UserStoriesTemplate(string title) => $"""
        # {title} — Acceptance Criteria

        ## Must Haves


        ## Quality Gates
        - Standalone review: ≥82%
        - Cumulative story score: ≥85%

        ## Voice Contract


        ## Open Questions

        """;
}
