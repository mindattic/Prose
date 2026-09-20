using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Maintains a compressed scene-to-scene memory chain. After each scene,
/// generates a 3-4 sentence summary and persists it so the chain survives
/// app restarts. The next scene gets the summary chain instead of the full
/// text — enabling long-form coherence without burning context tokens.
/// </summary>
public class NarrativeSummaryService
{
    private readonly ILlmService llm;
    private readonly IDbContextFactory<ProseDbContext>? dbFactory;

    // Running chain of summaries for the current node (loaded from DB on init)
    private readonly List<string> summaryChain = [];
    private Guid loadedNodeId = Guid.Empty;

    public NarrativeSummaryService(ILlmService llm, IDbContextFactory<ProseDbContext>? dbFactory = null)
    {
        this.llm = llm;
        this.dbFactory = dbFactory;
    }

    /// <summary>
    /// Load the persisted summary chain for <paramref name="nodeId"/> from the database.
    /// Call this once before writing beats on a node (typically from ProseWriterRouter).
    /// </summary>
    public async Task LoadAsync(Guid nodeId, CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty || nodeId == loadedNodeId || dbFactory == null) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entries = await db.NarrativeSummaryEntries
            .AsNoTracking()
            .Where(e => e.NodeId == nodeId)
            .OrderBy(e => e.SortKey)
            .Select(e => e.Summary)
            .ToListAsync(ct);

        summaryChain.Clear();
        summaryChain.AddRange(entries);
        loadedNodeId = nodeId;
    }

    /// <summary>The full summary chain formatted for injection into the next scene's prompt.</summary>
    public string GetSummaryChain()
    {
        if (summaryChain.Count == 0) return "";

        // Keep last 10 summaries to prevent unbounded growth
        var recent = summaryChain.Count > 10
            ? summaryChain.Skip(summaryChain.Count - 10).ToList()
            : summaryChain;

        return "STORY SO FAR (compressed summaries of previous scenes):\n"
            + string.Join("\n", recent.Select((s, i) => $"Scene {i + 1}: {s}"));
    }

    /// <summary>Compress a completed scene into a brief summary and persist it.</summary>
    public async Task SummarizeSceneAsync(
        string sceneText,
        Guid nodeId = default,
        Guid? beatId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sceneText)) return;

        var system = StorySummaryPrompt.Build("scene");

        var summary = await llm.GenerateAsync(system, sceneText, 0.3, 256, model: LlmModels.Haiku, ct: ct);
        await PersistSummaryAsync(summary.Trim(), nodeId, beatId, ct);
    }

    /// <summary>
    /// Persist an already-produced scene summary (no LLM call here) — split out of
    /// <see cref="SummarizeSceneAsync"/> so <see cref="BeatExtractionService"/> can reuse it
    /// after a single consolidated extraction call instead of this class firing its own LLM
    /// call too. <see cref="SummarizeSceneAsync"/> itself is unchanged.
    /// </summary>
    public async Task PersistSummaryAsync(string trimmedSummary, Guid nodeId, Guid? beatId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trimmedSummary)) return;

        summaryChain.Add(trimmedSummary);

        // Persist so the chain survives restarts (no-op when dbFactory is null, e.g. in unit tests)
        if (nodeId != Guid.Empty && dbFactory != null)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                await using var tx = await db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, ct);
                var sortKey = (await db.NarrativeSummaryEntries
                    .Where(e => e.NodeId == nodeId)
                    .MaxAsync(e => (int?)e.SortKey, ct) ?? 0) + 1;

                db.NarrativeSummaryEntries.Add(new NarrativeSummaryEntry
                {
                    Id         = Guid.CreateVersion7(),
                    NodeId     = nodeId,
                    BeatId     = beatId,
                    SortKey    = sortKey,
                    Summary    = trimmedSummary.Length > 2000 ? trimmedSummary[..2000] : trimmedSummary,
                    RecordedAt = DateTime.UtcNow,
                });
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { /* non-fatal — chain is still in-memory for this session */ }
        }
    }

    /// <summary>Clear the in-memory chain (does not delete DB entries).</summary>
    public void Reset()
    {
        summaryChain.Clear();
        loadedNodeId = Guid.Empty;
    }

    /// <summary>Get the number of scenes summarized.</summary>
    public int SceneCount => summaryChain.Count;
}
