using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Tracks open narrative threads — setups, promises, questions, and plants
/// auto-detected in each beat. Provides a context block for later beats so the
/// generator knows what it still owes the reader.
///
/// Detection and resolution are fire-and-forget (cheap Haiku calls) triggered
/// from ProseWriterRouter after each beat is generated.
/// </summary>
public class OpenThreadsService(
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILlmService llm)
{
    /// <summary>
    /// Detect new open threads introduced in this beat prose and register them.
    /// Non-fatal — failure is logged, not propagated.
    /// </summary>
    public async Task DetectAndRegisterAsync(
        Guid nodeId,
        Guid beatId,
        string prose,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prose)) return;

        var raw = await llm.GenerateAsync(
            system: """
                You are a continuity editor. Read the prose excerpt below and list ONLY new setups,
                promises, unresolved questions, wounds, or foreshadowing introduced in THIS beat that
                a reader will expect to be addressed later. Do NOT list things that are already resolved
                within this excerpt. Output one item per line, max 8 items, max 120 chars each.
                If nothing new is set up, output the single word NONE.
                """,
            user: prose,
            temperature: 0.2,
            maxTokens: 400,
            ct: ct);

        if (string.IsNullOrWhiteSpace(raw) || raw.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase))
            return;

        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Where(l => l.Length > 5 && !l.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                       .Take(8)
                       .ToList();

        if (lines.Count == 0) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        foreach (var line in lines)
        {
            db.NodeOpenThreads.Add(new NodeOpenThread
            {
                Id           = Guid.CreateVersion7(),
                NodeId     = nodeId,
                OriginBeatId = beatId,
                Description  = line.Length > 500 ? line[..500] : line,
                Category     = "promise",
                IsResolved   = false,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Check whether this beat's prose resolves any open threads and mark them resolved.
    /// Non-fatal — failure is logged, not propagated.
    /// </summary>
    public async Task MarkResolvedAsync(
        Guid nodeId,
        Guid beatId,
        string prose,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prose)) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var open = await db.NodeOpenThreads
            .Where(t => t.NodeId == nodeId && !t.IsResolved)
            .ToListAsync(ct);

        if (open.Count == 0) return;

        var threadList = string.Join("\n", open.Select((t, i) => $"{i + 1}. {t.Description}"));
        var raw = await llm.GenerateAsync(
            system: """
                You are a continuity editor. Given a list of open narrative threads and a prose excerpt,
                output ONLY the 1-based line numbers of threads that are fully resolved (closed, paid off,
                or definitively answered) by this excerpt. One number per line. If none, output NONE.
                """,
            user: $"OPEN THREADS:\n{threadList}\n\nPROSE:\n{prose}",
            temperature: 0.2,
            maxTokens: 100,
            ct: ct);

        if (string.IsNullOrWhiteSpace(raw) || raw.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase))
            return;

        var resolved = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => int.TryParse(l.Trim().TrimEnd('.'), out var n) ? n : 0)
            .Where(n => n >= 1 && n <= open.Count)
            .Distinct()
            .ToList();

        foreach (var idx in resolved)
        {
            var thread = open[idx - 1];
            thread.IsResolved     = true;
            thread.ResolvedBeatId = beatId;
            thread.UpdatedAt      = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Build a formatted open-threads context block for injection into BeatContext.
    /// Returns empty string when no open threads exist.
    /// </summary>
    public async Task<string> BuildContextAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var open = await db.NodeOpenThreads
            .AsNoTracking()
            .Where(t => t.NodeId == nodeId && !t.IsResolved)
            .OrderBy(t => t.CreatedAt)
            .Take(15)
            .ToListAsync(ct);

        if (open.Count == 0) return "";

        var lines = open.Select((t, i) => $"  {i + 1}. {t.Description}");
        return "OPEN NARRATIVE THREADS (promises and setups the story still owes the reader):\n"
             + string.Join("\n", lines)
             + "\nAddress or advance at least one of the above in the beat you write.";
    }
}
