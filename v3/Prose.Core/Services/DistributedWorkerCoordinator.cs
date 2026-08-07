using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Central coordinator for distributed work.  Remote workers (RunPod pods, local GPU boxes,
/// etc.) claim batches via the REST API, run their local LLM, and POST results back.
/// This service is the only layer that writes to EntityReviews, NodeReviews, Edges, or Beats.
///
/// Personas come from PersonaLibrary (in-process static registry, not DB).
/// Claim timeout: 15 minutes.  If a worker dies mid-batch the items auto-release.
/// </summary>
public class DistributedWorkerCoordinator
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<DistributedWorkerCoordinator> log;

    private static readonly TimeSpan ClaimTimeout = TimeSpan.FromMinutes(15);

    public DistributedWorkerCoordinator(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<DistributedWorkerCoordinator> log)
    {
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    // ── Populate ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds the queue with entity-review work items for all entities of the given types
    /// that don't already have a completed summary or queue entry.
    /// Workers sample personas from PersonaLibrary (in-process) — not embedded in payload.
    /// </summary>
    public async Task<int> PopulateEntityReviewAsync(
        IReadOnlyList<string> entityTypes, int ballots = 10, int proseCount = 2,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var alreadyQueued   = await db.DistributedWorkQueue.Where(q => q.WorkType == "entity-review").Select(q => q.TargetId).ToHashSetAsync(ct);
        var alreadyReviewed = await db.EntityReviewSummaries.Select(s => s.EntityId).ToHashSetAsync(ct);

        int added = 0;
        foreach (var type in entityTypes)
        {
            var entities = await db.Entities
                .Where(e => e.EntityType == type && e.IsActive)
                .Select(e => new { e.Id, e.Name, e.Description })
                .ToListAsync(ct);

            foreach (var ent in entities)
            {
                var eid = ent.Id.ToString("N");
                if (alreadyQueued.Contains(eid) || alreadyReviewed.Contains(eid)) continue;

                var payload = JsonSerializer.Serialize(new
                {
                    entityId    = eid,
                    entityType  = type,
                    entityName  = ent.Name ?? "",
                    description = ent.Description ?? "",
                    ballots,
                    proseCount,
                });

                db.DistributedWorkQueue.Add(new DistributedWorkQueue
                {
                    Id          = Guid.CreateVersion7(),
                    WorkType    = "entity-review",
                    TargetId    = eid,
                    TargetType  = type,
                    TargetName  = ent.Name ?? "",
                    PayloadJson = payload,
                    Status      = "pending",
                    CreatedAt   = DateTime.UtcNow,
                });
                added++;
            }
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("DistributedCoordinator: queued {N} entity-review items", added);
        return added;
    }

    /// <summary>Seeds the queue with node-review work items.</summary>
    public async Task<int> PopulateNodeReviewAsync(
        IReadOnlyList<Guid>? nodeIds = null, int readers = 5,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var alreadyQueued = await db.DistributedWorkQueue
            .Where(q => q.WorkType == "node-review")
            .Select(q => q.TargetId)
            .ToHashSetAsync(ct);

        IQueryable<Node> query = db.Nodes;
        if (nodeIds?.Count > 0) query = query.Where(s => nodeIds.Contains(s.Id));

        var nodes = await query
            .Select(s => new { s.Id, s.Title, s.Slug })
            .ToListAsync(ct);

        int added = 0;
        foreach (var node in nodes)
        {
            var sid = node.Id.ToString("N");
            if (alreadyQueued.Contains(sid)) continue;

            // Load beat texts via junction; SS-A43: expand to chapter children for book nodes.
            var childIds = await db.Nodes.AsNoTracking()
                .Where(n => n.ParentNodeId == node.Id)
                .Select(n => n.Id).ToListAsync(ct);
            var searchIds = childIds.Count > 0 ? childIds : new List<Guid> { node.Id };
            var beatTexts = await db.BeatNodes
                .Where(sb => searchIds.Contains(sb.NodeId) && sb.IsEnabled)
                .OrderBy(sb => sb.SortKey)
                .Select(sb => sb.Beat!.Text ?? "")
                .ToListAsync(ct);

            var fullText = string.Join("\n\n---\n\n", beatTexts.Where(t => !string.IsNullOrWhiteSpace(t)));
            if (string.IsNullOrWhiteSpace(fullText)) continue;

            var payload = JsonSerializer.Serialize(new
            {
                nodeId    = sid,
                nodeSlug  = node.Slug ?? "",
                nodeTitle = node.Title ?? "",
                nodeText  = fullText,
                readers,
            });

            db.DistributedWorkQueue.Add(new DistributedWorkQueue
            {
                Id          = Guid.CreateVersion7(),
                WorkType    = "node-review",
                TargetId    = sid,
                TargetType  = "node",
                TargetName  = node.Title ?? node.Slug ?? sid,
                PayloadJson = payload,
                Status      = "pending",
                CreatedAt   = DateTime.UtcNow,
            });
            added++;
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("DistributedCoordinator: queued {N} node-review items", added);
        return added;
    }

    /// <summary>Seeds the queue with beat-write items for beats that have no text yet.</summary>
    public async Task<int> PopulateBeatWriteAsync(
        IReadOnlyList<Guid>? nodeIds = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var alreadyQueued = await db.DistributedWorkQueue
            .Where(q => q.WorkType == "beat-write")
            .Select(q => q.TargetId)
            .ToHashSetAsync(ct);

        // Query via the BeatNode junction — beats are m:m with nodes.
        var query = db.BeatNodes
            .Where(sb => sb.IsEnabled
                      && (sb.Beat!.Text == null || sb.Beat.Text == ""));

        if (nodeIds?.Count > 0)
            query = query.Where(sb => nodeIds.Contains(sb.NodeId));

        var BeatNodeRows = await query
            .OrderBy(sb => sb.NodeId).ThenBy(sb => sb.SortKey)
            .Select(sb => new
            {
                BeatId   = sb.BeatId,
                NodeId   = sb.NodeId,
                Slug     = sb.Node!.Slug,
                Title    = sb.Node.Title,
                SortKey  = sb.SortKey,
                Description = sb.Beat!.Description,
                Subtext  = sb.Beat.Subtext,
            })
            .ToListAsync(ct);

        // Group by node so we can compute position (1-based index, approximated by SortKey rank).
        var byNode = BeatNodeRows.GroupBy(sb => sb.NodeId);
        int added = 0;
        foreach (var grp in byNode)
        {
            var orderedBeats = grp.OrderBy(sb => sb.SortKey).ToList();
            int total = orderedBeats.Count;
            for (int i = 0; i < total; i++)
            {
                var beat = orderedBeats[i];
                var bid  = beat.BeatId.ToString("N");
                if (alreadyQueued.Contains(bid)) continue;

                var payload = JsonSerializer.Serialize(new
                {
                    beatId     = bid,
                    nodeId   = beat.NodeId.ToString("N"),
                    nodeSlug = beat.Slug ?? "",
                    nodeTitle= beat.Title ?? "",
                    beatIndex   = i,
                    totalBeats  = total,
                    beatGoal    = beat.Description ?? "",
                    beatSubtext = beat.Subtext ?? "",
                });

                db.DistributedWorkQueue.Add(new DistributedWorkQueue
                {
                    Id          = Guid.CreateVersion7(),
                    WorkType    = "beat-write",
                    TargetId    = bid,
                    TargetType  = "beat",
                    TargetName  = $"{beat.Title ?? beat.Slug ?? ""}: beat {i + 1}/{total}",
                    PayloadJson = payload,
                    Status      = "pending",
                    CreatedAt   = DateTime.UtcNow,
                });
                added++;
            }
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("DistributedCoordinator: queued {N} beat-write items", added);
        return added;
    }

    // ── Claim ─────────────────────────────────────────────────────────────────

    public async Task<List<WorkItem>> ClaimBatchAsync(
        string workerId, string workType, int count, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var timeout = DateTime.UtcNow - ClaimTimeout;

        var timedOut = await db.DistributedWorkQueue
            .Where(q => q.WorkType == workType && q.Status == "claimed"
                     && q.ClaimedAt < timeout && q.RetryCount < 3)
            .ToListAsync(ct);
        foreach (var t in timedOut)
        { t.Status = "pending"; t.ClaimedBy = null; t.ClaimedAt = null; t.RetryCount++; }

        var dead = await db.DistributedWorkQueue
            .Where(q => q.WorkType == workType && q.Status == "claimed"
                     && q.ClaimedAt < timeout && q.RetryCount >= 3)
            .ToListAsync(ct);
        foreach (var d in dead) d.Status = "failed";

        var items = await db.DistributedWorkQueue
            .Where(q => q.WorkType == workType && q.Status == "pending")
            .OrderBy(q => q.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var item in items)
        { item.Status = "claimed"; item.ClaimedBy = workerId; item.ClaimedAt = now; }

        await db.SaveChangesAsync(ct);

        return items.Select(i => new WorkItem(i.Id, i.WorkType, i.TargetId, i.TargetType, i.TargetName, i.PayloadJson)).ToList();
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    public async Task<WorkSubmitResult> SubmitAsync(WorkerResult result, CancellationToken ct = default)
    {
        return result.WorkType switch
        {
            "entity-review" => await SubmitEntityReviewAsync(result, ct),
            "node-review" => await SubmitNodeReviewAsync(result, ct),
            "beat-write"    => await SubmitBeatWriteAsync(result, ct),
            _               => new WorkSubmitResult(0, 1, $"Unknown WorkType '{result.WorkType}'"),
        };
    }

    private async Task<WorkSubmitResult> SubmitEntityReviewAsync(WorkerResult result, CancellationToken ct)
    {
        if (result.EntityBallots == null || result.EntityBallots.Count == 0)
            return new WorkSubmitResult(0, 0, "no ballots");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        int saved = 0, failed = 0;

        foreach (var b in result.EntityBallots)
        {
            try
            {
                db.EntityReviews.Add(new EntityReview
                {
                    Id             = Guid.CreateVersion7(),
                    EntityId       = b.EntityId,
                    EntityType     = b.EntityType,
                    EntityName     = b.EntityName,
                    PersonaId      = b.PersonaId,
                    PersonaName    = b.PersonaName,
                    PersonaBlurb   = b.PersonaBlurb,
                    ProviderId     = "local",
                    Model          = result.Model,
                    Score          = Math.Clamp(b.Score, 1, 100),
                    ReviewText     = b.ReviewText ?? "",
                    Improvements   = b.Improvements,
                    Contradictions = b.Contradictions,
                    ContentHash    = b.ContentHash,
                    ReviewedAt     = DateTime.UtcNow,
                    CreatedAt      = DateTime.UtcNow,
                    UpdatedAt      = DateTime.UtcNow,
                });
                saved++;
            }
            catch (Exception ex) { log.LogWarning(ex, "Submit entity-review: ballot error for {E}", b.EntityId); failed++; }
        }
        await db.SaveChangesAsync(ct);

        var byEntity = result.EntityBallots.GroupBy(b => b.EntityId);
        foreach (var grp in byEntity)
        {
            var first = grp.First();
            await UpsertEntitySummaryAsync(db, grp.Key, first.EntityType, first.EntityName, first.ContentHash, ct);
        }

        foreach (var edge in result.Edges ?? [])
            await TryAddEdgeAsync(db, edge, ct);
        await db.SaveChangesAsync(ct);

        await MarkQueueDoneAsync(db, result.QueueId, ct);
        return new WorkSubmitResult(saved, failed);
    }

    private async Task<WorkSubmitResult> SubmitNodeReviewAsync(WorkerResult result, CancellationToken ct)
    {
        if (result.NodeReview == null)
            return new WorkSubmitResult(0, 1, "no node review");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var sr = result.NodeReview;

        if (!Guid.TryParse(sr.NodeId, out var nodeGuid))
            return new WorkSubmitResult(0, 1, "bad NodeId");

        int saved = 0;
        foreach (var vote in sr.PersonaVotes ?? [])
        {
            db.NodeReviews.Add(new NodeReview
            {
                Id             = Guid.CreateVersion7(),
                NodeId       = nodeGuid,
                PersonaId      = vote.PersonaId,
                PersonaName    = vote.PersonaName,
                PersonaBlurb   = vote.PersonaBlurb,
                ProviderId     = "local",
                Model          = result.Model,
                Score          = Math.Clamp(vote.Score, 1, 100),
                Improvements   = vote.Improvements,
                Contradictions = vote.Contradictions,
                ContentHash    = sr.ContentHash ?? "",
                ReviewedAt     = DateTime.UtcNow,
                CreatedAt      = DateTime.UtcNow,
                UpdatedAt      = DateTime.UtcNow,
            });
            saved++;
        }
        await db.SaveChangesAsync(ct);
        await MarkQueueDoneAsync(db, result.QueueId, ct);
        return new WorkSubmitResult(saved, 0);
    }

    private async Task<WorkSubmitResult> SubmitBeatWriteAsync(WorkerResult result, CancellationToken ct)
    {
        if (result.BeatWrite == null)
            return new WorkSubmitResult(0, 1, "no beat write");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var bw = result.BeatWrite;

        if (!Guid.TryParse(bw.BeatId, out var beatGuid))
            return new WorkSubmitResult(0, 1, "bad BeatId");

        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatGuid, ct);
        if (beat == null)
            return new WorkSubmitResult(0, 1, "beat not found");

        if (!string.IsNullOrWhiteSpace(beat.Text))
        {
            log.LogWarning("SubmitBeatWrite: beat {B} already has text — skipping", beatGuid);
            await MarkQueueDoneAsync(db, result.QueueId, ct);
            return new WorkSubmitResult(0, 0, "already written");
        }

        beat.Text      = TextSanitizerService.Sanitize(bw.ProseText);
        // Written prose must carry its hash, or review-score invalidation cannot tell this
        // beat from an unwritten one. The DbContext enforces this on save too; setting it
        // here keeps the intent visible at the call site.
        beat.TextHash  = Beat.ComputeHash(beat.Text);
        beat.UpdatedAt = DateTime.UtcNow;
        beat.Version   = beat.Version + 1;
        await db.SaveChangesAsync(ct);

        await MarkQueueDoneAsync(db, result.QueueId, ct);
        return new WorkSubmitResult(1, 0);
    }

    // ── Status ────────────────────────────────────────────────────────────────

    public async Task<List<WorkTypeStatus>> GetStatusAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.DistributedWorkQueue
            .GroupBy(q => new { q.WorkType, q.Status })
            .Select(g => new WorkTypeStatus(g.Key.WorkType, g.Key.Status, g.Count()))
            .ToListAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static async Task UpsertEntitySummaryAsync(ProseDbContext db,
        string entityId, string entityType, string entityName, string contentHash, CancellationToken ct)
    {
        var allReviews = await db.EntityReviews.Where(r => r.EntityId == entityId).ToListAsync(ct);
        if (allReviews.Count == 0) return;
        var avg      = allReviews.Average(r => (double)r.Score);
        var dist     = allReviews.GroupBy(r => r.Score / 10 * 10).ToDictionary(g => g.Key, g => g.Count());
        var distJson = JsonSerializer.Serialize(dist);
        var existing = await db.EntityReviewSummaries.FirstOrDefaultAsync(s => s.EntityId == entityId, ct);
        if (existing == null)
            db.EntityReviewSummaries.Add(new EntityReviewSummary
            {
                Id = Guid.CreateVersion7(), EntityId = entityId, EntityType = entityType,
                EntityName = entityName, ReviewCount = allReviews.Count, AvgScore = Math.Round(avg, 2),
                ScoreDistributionJson = distJson, ContentHash = contentHash, GeneratedAt = DateTime.UtcNow,
            });
        else
        {
            existing.ReviewCount = allReviews.Count; existing.AvgScore = Math.Round(avg, 2);
            existing.ScoreDistributionJson = distJson; existing.ContentHash = contentHash;
            existing.GeneratedAt = DateTime.UtcNow;
        }
    }

    private static async Task TryAddEdgeAsync(ProseDbContext db, EdgeResult edge, CancellationToken ct)
    {
        if (!Guid.TryParse(edge.SourceEntityId, out var srcId)
            || !Guid.TryParse(edge.TargetEntityId, out var tgtId)) return;
        var tgtExists = await db.Entities.AnyAsync(e => e.Id == tgtId, ct);
        if (!tgtExists) return;
        var exists = await db.Edges.AnyAsync(e =>
            e.SourceId == srcId && e.TargetId == tgtId && e.RelationType == edge.RelationType, ct);
        if (exists) return;
        var universeId = await db.Entities.Where(e => e.Id == srcId).Select(e => e.UniverseId).FirstOrDefaultAsync(ct);
        db.Edges.Add(new Edge
        {
            SourceId     = srcId,
            TargetId     = tgtId,
            RelationType = edge.RelationType,
            Description  = edge.Description,
            Sentiment    = edge.Sentiment ?? "neutral",
            Weight       = edge.Confidence,
            Source       = "review:entity-scoring",
            UniverseId   = universeId,
        });
    }

    private static async Task MarkQueueDoneAsync(ProseDbContext db, Guid queueId, CancellationToken ct)
    {
        var item = await db.DistributedWorkQueue.FirstOrDefaultAsync(q => q.Id == queueId, ct);
        if (item == null) return;
        item.Status      = "done";
        item.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record WorkItem(Guid QueueId, string WorkType, string TargetId, string TargetType, string TargetName, string? PayloadJson);

public record WorkTypeStatus(string WorkType, string Status, int Count);

public record WorkSubmitResult(int Saved, int Failed, string? Note = null);

public class WorkerResult
{
    public string  WorkType  { get; set; } = "";
    public string  WorkerId  { get; set; } = "";
    public string  Model     { get; set; } = "";
    public Guid    QueueId   { get; set; }
    public bool    Failed    { get; set; }
    public string? ErrorMessage { get; set; }

    public List<BallotResult>? EntityBallots { get; set; }
    public List<EdgeResult>?   Edges         { get; set; }
    public NodeReviewResult? NodeReview  { get; set; }
    public BeatWriteResult?    BeatWrite     { get; set; }
}

public class NodeReviewResult
{
    public string  NodeId    { get; set; } = "";
    public string? ContentHash { get; set; }
    public List<PersonaVoteResult>? PersonaVotes { get; set; }
}

public class PersonaVoteResult
{
    public string  PersonaId     { get; set; } = "";
    public string  PersonaName   { get; set; } = "";
    public string? PersonaBlurb  { get; set; }
    public int     Score         { get; set; }
    public string? Improvements  { get; set; }
    public string? Contradictions{ get; set; }
}

public class BeatWriteResult
{
    public string  BeatId    { get; set; } = "";
    public string  ProseText { get; set; } = "";
}

public class BallotResult
{
    public string EntityId      { get; set; } = "";
    public string EntityType    { get; set; } = "";
    public string EntityName    { get; set; } = "";
    public string PersonaId     { get; set; } = "";
    public string PersonaName   { get; set; } = "";
    public string? PersonaBlurb { get; set; }
    public int    Score         { get; set; }
    public string? ReviewText   { get; set; }
    public string? Improvements { get; set; }
    public string? Contradictions { get; set; }
    public string ContentHash   { get; set; } = "";
}

public class EdgeResult
{
    public string SourceEntityId { get; set; } = "";
    public string TargetEntityId { get; set; } = "";
    public string RelationType   { get; set; } = "";
    public string? Description   { get; set; }
    public string? Sentiment     { get; set; }
    public double  Confidence    { get; set; }
}
