using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

// ── StrandSpineService ─────────────────────────────────────────────────────
// Manages the per-strand narrative spine: the bible (already on Strand),
// user stories (new column), amendments (append-only log), and version pins
// (bridge: StrandVersion ↔ spine hashes).
//
// Invariants:
//   - AmendmentSeqNo is 1-based, monotonically increasing per strand.
//   - PinVersionAsync creates exactly one pin per (StrandId, StrandVersion).
//   - ScaffoldAsync is idempotent — safe to call on every CreateStrand.
// ──────────────────────────────────────────────────────────────────────────

public class StrandSpineService
{
    public record SpineDto(
        Guid         StrandId,
        string?      Bible,
        DateTime?    BibleUpdatedAt,
        string?      UserStories,
        DateTime?    UserStoriesUpdatedAt,
        List<StrandAmendment>     Amendments,
        StrandSpineVersion?       LatestPin);

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public StrandSpineService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    // ── Scaffold (called by CreateStrand) ─────────────────────────────────

    public async Task ScaffoldAsync(Guid strandId, string title, bool bibleAlreadySet, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(x => x.Id == strandId, ct);
        if (strand == null) return;

        bool changed = false;

        if (!bibleAlreadySet && string.IsNullOrWhiteSpace(strand.StrandBible))
        {
            strand.StrandBible = BibleTemplate(title);
            strand.StrandBibleGeneratedAt = DateTime.UtcNow;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(strand.StrandUserStories))
        {
            strand.StrandUserStories = UserStoriesTemplate(title);
            strand.StrandUserStoriesUpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        if (changed)
        {
            strand.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Get full spine ─────────────────────────────────────────────────────

    public async Task<SpineDto?> GetSpineAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(x => x.Id == strandId, ct);
        if (strand == null) return null;

        var amendments = await db.StrandAmendments
            .Where(x => x.StrandId == strandId)
            .OrderBy(x => x.SequenceNo)
            .ToListAsync(ct);

        var latestPin = await db.StrandSpineVersions
            .Where(x => x.StrandId == strandId)
            .OrderByDescending(x => x.StrandVersion)
            .FirstOrDefaultAsync(ct);

        return new SpineDto(
            strandId,
            strand.StrandBible,
            strand.StrandBibleGeneratedAt,
            strand.StrandUserStories,
            strand.StrandUserStoriesUpdatedAt,
            amendments,
            latestPin);
    }

    // ── Update user stories ────────────────────────────────────────────────

    public async Task SetUserStoriesAsync(Guid strandId, string content, string updatedBy, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(x => x.Id == strandId, ct);
        if (strand == null) throw new InvalidOperationException($"Strand {strandId} not found.");

        strand.StrandUserStories = content;
        strand.StrandUserStoriesUpdatedAt = DateTime.UtcNow;
        strand.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Append amendment ──────────────────────────────────────────────────

    public async Task<StrandAmendment> AppendAmendmentAsync(
        Guid strandId, string summary, string body, string createdBy, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var maxSeq = await db.StrandAmendments
            .Where(x => x.StrandId == strandId)
            .Select(x => (int?)x.SequenceNo)
            .MaxAsync(ct) ?? 0;

        var seq  = maxSeq + 1;
        var code = $"SA-{seq}";

        var row = new StrandAmendment
        {
            Id         = Guid.NewGuid(),
            StrandId   = strandId,
            SequenceNo = seq,
            Code       = code,
            Summary    = summary,
            Body       = body,
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = createdBy,
        };

        db.StrandAmendments.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    // ── List amendments ───────────────────────────────────────────────────

    public async Task<List<StrandAmendment>> ListAmendmentsAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.StrandAmendments
            .Where(x => x.StrandId == strandId)
            .OrderBy(x => x.SequenceNo)
            .ToListAsync(ct);
    }

    // ── Pin spine version ─────────────────────────────────────────────────

    public async Task<StrandSpineVersion> PinVersionAsync(
        Guid strandId, string notes, string pinnedBy, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(x => x.Id == strandId, ct);
        if (strand == null) throw new InvalidOperationException($"Strand {strandId} not found.");

        var amendmentCount = await db.StrandAmendments
            .Where(x => x.StrandId == strandId)
            .Select(x => (int?)x.SequenceNo)
            .MaxAsync(ct) ?? 0;

        var bibleHash       = Hash(strand.StrandBible ?? "");
        var userStoriesHash = Hash(strand.StrandUserStories ?? "");

        // Upsert: if a pin already exists for this version, update it.
        var existing = await db.StrandSpineVersions
            .FirstOrDefaultAsync(x => x.StrandId == strandId && x.StrandVersion == strand.Version, ct);

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

        var pin = new StrandSpineVersion
        {
            Id               = Guid.NewGuid(),
            StrandId         = strandId,
            StrandVersion    = strand.Version,
            BibleHash        = bibleHash,
            UserStoriesHash  = userStoriesHash,
            AmendmentCount   = amendmentCount,
            PinnedAt         = DateTime.UtcNow,
            PinnedBy         = pinnedBy,
            Notes            = notes,
        };
        db.StrandSpineVersions.Add(pin);
        await db.SaveChangesAsync(ct);
        return pin;
    }

    // ── List all pins ─────────────────────────────────────────────────────

    public async Task<List<StrandSpineVersion>> ListPinsAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.StrandSpineVersions
            .Where(x => x.StrandId == strandId)
            .OrderByDescending(x => x.StrandVersion)
            .ToListAsync(ct);
    }

    // ── Drift check ───────────────────────────────────────────────────────
    // Returns true when the current bible or user_stories hash differs from
    // the most recent pin — meaning prose was written against stale spine docs.

    public async Task<(bool drifted, string reason)> CheckDriftAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(x => x.Id == strandId, ct);
        if (strand == null) return (false, "strand not found");

        var latestPin = await db.StrandSpineVersions
            .Where(x => x.StrandId == strandId)
            .OrderByDescending(x => x.StrandVersion)
            .FirstOrDefaultAsync(ct);

        if (latestPin == null) return (false, "no pin yet");

        var bibleHash       = Hash(strand.StrandBible ?? "");
        var userStoriesHash = Hash(strand.StrandUserStories ?? "");

        var reasons = new List<string>();
        if (latestPin.BibleHash != bibleHash)       reasons.Add("bible changed since last pin");
        if (latestPin.UserStoriesHash != userStoriesHash) reasons.Add("user_stories changed since last pin");

        var amendmentCount = await db.StrandAmendments
            .Where(x => x.StrandId == strandId)
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
        # {title} — Strand Bible

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
