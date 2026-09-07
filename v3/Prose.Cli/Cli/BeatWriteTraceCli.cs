using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --beat-write-trace (--beat-id &lt;guid&gt; | --last) [--json]
///
/// Single-source-writer RFC, step one (2026-09-07): make one beat write observable. Read-only,
/// deterministic, zero LLM cost. Answers, from the data rather than from reading the router:
/// how many LLM and embedding calls one <c>ProseWriterRouter.WriteAsync</c> made, which stage
/// made each, what each cost, how long each stage took, and which stages were skipped by their
/// gate or threw.
///
/// <para>Sources: <see cref="LlmCallHistory"/> (every chat + embedding call tagged with the
/// ambient beat/stage by <c>LlmRouter</c> / <c>EmbeddingService</c>), <see cref="BeatWriteStageLog"/>
/// (per-stage order/phase/wall-time/outcome, written by the router at the end of the post-write
/// cluster, terminated by a <see cref="BeatWriteStageLog.CompleteMarker"/> row), and
/// <see cref="BeatServiceLog"/> (the existing coverage rows — applicable/active/block size).
/// Calls whose <c>Stage</c> is null are printed as UNATTRIBUTED, never dropped: they are a
/// plumbing gap to fix, and hiding them would make the count a lie.</para>
///
/// <para>Explicit-id lookup via IgnoreQueryFilters() — a beat id names its own universe; no
/// ambient scope to resolve (same exemption shape as --beat-archive).</para>
/// </summary>
public static class BeatWriteTraceCli
{
    private sealed record CallRow(string Stage, DateTime At, string Provider, string Model, bool Success,
        int InputTokens, int OutputTokens, double Cost, int? ElapsedMs, string? Error);

    private sealed record StageGroup(string Stage, int Calls, int ChatCalls, int EmbedCalls, int Failed,
        int InputTokens, int OutputTokens, double Cost, int ElapsedMs, string Models);

    private sealed record StageRow(int Ordinal, string Phase, string Stage, int ElapsedMs, bool Succeeded,
        int LlmCalls, double LlmCost, bool? CoverageApplicable, bool? CoverageActive, int? CoverageChars);

    private sealed record Report(
        Guid BeatId, int BeatNumber, string? BeatTitle, string? Book, string? Chapter, string? Universe,
        string? LastWriteReason, int Version, int TextChars,
        bool Complete, DateTime? WriteStartedAt, DateTime? WriteFinishedAt, int? WallMs,
        int TotalCalls, int ChatCalls, int EmbedCalls, int FailedCalls, int InputTokens, int OutputTokens, double TotalCost,
        int UnattributedCalls,
        IReadOnlyList<StageGroup> CallsByStage,
        IReadOnlyList<StageRow> Stages,
        IReadOnlyList<string> SkippedByGate,
        IReadOnlyList<CallRow> Calls);

    private const string Unattributed = "(unattributed)";

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        Guid? beatId = null;
        bool last = args.Contains("--last");
        bool json = args.Contains("--json");
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--beat-id" && i + 1 < args.Length && Guid.TryParse(args[i + 1], out var g)) beatId = g;

        if (beatId == null && !last)
        {
            Console.Error.WriteLine("Usage: prose --beat-write-trace (--beat-id <guid> | --last) [--json]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (beatId == null)
        {
            beatId = await db.BeatWriteStageLogs.AsNoTracking().IgnoreQueryFilters()
                .OrderByDescending(s => s.WrittenAt).ThenByDescending(s => s.Id)
                .Select(s => (Guid?)s.BeatId).FirstOrDefaultAsync();
            if (beatId == null)
            {
                Console.Error.WriteLine("[beat-write-trace] COULD NOT LOOK — no BeatWriteStageLog rows exist yet (no instrumented write has run).");
                return 2;
            }
        }

        var beat = await db.Beats.AsNoTracking().IgnoreQueryFilters()
            .Where(b => b.Id == beatId.Value)
            .Select(b => new { b.Id, b.Number, b.Title, b.LastWriteReason, b.Version, TextChars = b.Text.Length })
            .FirstOrDefaultAsync();
        if (beat == null)
        {
            Console.Error.WriteLine($"[beat-write-trace] Beat {beatId} not found.");
            return 2;
        }

        var chapter = await (
            from bn in db.BeatNodes.AsNoTracking().IgnoreQueryFilters()
            join n in db.Nodes.AsNoTracking().IgnoreQueryFilters() on bn.NodeId equals n.Id
            where bn.BeatId == beatId.Value
            select new { n.Id, n.Title, n.ParentNodeId, n.UniverseId }).FirstOrDefaultAsync();
        string? bookTitle = null, universe = null;
        if (chapter?.ParentNodeId != null)
        {
            bookTitle = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => n.Id == chapter.ParentNodeId).Select(n => n.Title).FirstOrDefaultAsync();
        }
        if (chapter != null)
        {
            universe = await db.Universes.AsNoTracking().IgnoreQueryFilters()
                .Where(u => u.Id == chapter.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync();
        }

        // Stage log: the most recent write's batch (rows of one write share a WrittenAt).
        var stageRowsAll = await db.BeatWriteStageLogs.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.BeatId == beatId.Value)
            .OrderByDescending(s => s.WrittenAt).ThenBy(s => s.Ordinal)
            .ToListAsync();
        var latestBatchAt = stageRowsAll.Count > 0 ? stageRowsAll[0].WrittenAt : (DateTime?)null;
        var stageRows = latestBatchAt == null
            ? []
            : stageRowsAll.Where(s => s.WrittenAt == latestBatchAt.Value).OrderBy(s => s.Ordinal).ToList();
        var complete = stageRows.Any(s => s.Stage == BeatWriteStageLog.CompleteMarker);

        // LLM calls for this beat. Restrict to the window of the latest write when we can tell
        // where it started (a beat written twice would otherwise merge both writes).
        var callsQ = db.LlmCallHistories.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.BeatId == beatId.Value);
        var callsAll = await callsQ.OrderBy(c => c.At).ToListAsync();
        DateTime? writeStart = null;
        if (latestBatchAt != null && stageRows.Count > 0)
        {
            var totalStageMs = stageRows.Sum(s => (long)s.ElapsedMs);
            // Stage log is written at the END of the cluster; calls for this write happened in
            // [end − Σstage − slack, end]. Generous slack — the goal is to exclude an EARLIER
            // write of the same beat, not to be tight.
            writeStart = latestBatchAt.Value.AddMilliseconds(-(totalStageMs + 120_000));
        }
        var calls = writeStart == null ? callsAll : callsAll.Where(c => c.At >= writeStart.Value).ToList();

        var callRows = calls.Select(c => new CallRow(
            c.Stage ?? Unattributed, c.At, c.ProviderId, c.Model, c.Success,
            c.InputTokens, c.OutputTokens, c.Cost, c.ElapsedMs, c.ErrorMessage)).ToList();

        static bool IsEmbed(string provider) => provider is EmbeddingService.OpenAiProviderId or EmbeddingService.LocalProviderId;

        var byStage = callRows
            .GroupBy(c => c.Stage)
            .Select(g => new StageGroup(
                g.Key, g.Count(),
                g.Count(c => !IsEmbed(c.Provider)), g.Count(c => IsEmbed(c.Provider)), g.Count(c => !c.Success),
                g.Sum(c => c.InputTokens), g.Sum(c => c.OutputTokens), g.Sum(c => c.Cost), g.Sum(c => c.ElapsedMs ?? 0),
                string.Join(", ", g.Select(c => $"{c.Provider}:{c.Model}").Distinct())))
            .OrderByDescending(g => g.Cost).ThenByDescending(g => g.Calls)
            .ToList();

        // Coverage rows (existing BeatServiceLog) — the latest batch for this beat.
        var coverageAll = await db.BeatServiceLogs.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.BeatId == beatId.Value)
            .OrderByDescending(s => s.WrittenAt)
            .ToListAsync();
        var coverage = new List<BeatServiceLog>();
        if (coverageAll.Count > 0)
        {
            var newest = coverageAll[0].WrittenAt;
            // One write's coverage rows land in two calls a few seconds apart (main array, then
            // ContinuityEnforcer) — accept a 10-minute window ending at the newest row.
            coverage = coverageAll.Where(s => s.WrittenAt >= newest.AddMinutes(-10)).ToList();
        }

        // Stage table: join each traced stage to its LLM calls (exact stage name, or the stage
        // as the leading segment of a nested "outer/inner" name) and to a coverage row by loose
        // name match (the two vocabularies were never unified).
        var stageTable = new List<StageRow>();
        foreach (var s in stageRows.Where(s => s.Stage != BeatWriteStageLog.CompleteMarker))
        {
            var mine = callRows.Where(c => c.Stage == s.Stage || c.Stage.StartsWith(s.Stage + "/", StringComparison.Ordinal)).ToList();
            var cov = coverage.FirstOrDefault(c => NamesMatch(c.Service, s.Stage));
            stageTable.Add(new StageRow(s.Ordinal, s.Phase, s.Stage, s.ElapsedMs, s.Succeeded,
                mine.Count, mine.Sum(c => c.Cost), cov?.WasApplicable, cov?.WasActive, cov?.BlockSizeChars));
        }

        var skipped = coverage.Where(c => !c.WasApplicable).Select(c => c.Service).Distinct().OrderBy(x => x).ToList();

        var wallMs = stageRows.Count > 0 ? stageRows.Sum(s => s.ElapsedMs) : (int?)null;
        var report = new Report(
            beat.Id, beat.Number, beat.Title, bookTitle, chapter?.Title, universe,
            beat.LastWriteReason, beat.Version, beat.TextChars,
            complete, writeStart, latestBatchAt, wallMs,
            callRows.Count, callRows.Count(c => !IsEmbed(c.Provider)), callRows.Count(c => IsEmbed(c.Provider)),
            callRows.Count(c => !c.Success), callRows.Sum(c => c.InputTokens), callRows.Sum(c => c.OutputTokens), callRows.Sum(c => c.Cost),
            callRows.Count(c => c.Stage == Unattributed),
            byStage, stageTable, skipped, callRows);

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Print(report);
        return 0;
    }

    private static bool NamesMatch(string coverageService, string stage)
    {
        static string Norm(string s) => new(s.Where(char.IsLetterOrDigit).ToArray());
        var a = Norm(coverageService).ToLowerInvariant();
        var b = Norm(stage).ToLowerInvariant();
        if (a.Length == 0 || b.Length == 0) return false;
        // "SceneContextAssembler" vs "SceneContextAssembler"; "Consequence" vs "ConsequenceService";
        // "ContinuityEnforcer" vs "ContinuityEnforcer guidance" is NOT a match on purpose (b has
        // extra words) — only prefix-with-Service-suffix is tolerated.
        return a == b || b == a + "service" || a == b + "service";
    }

    private static void Print(Report r)
    {
        Console.WriteLine($"BEAT WRITE TRACE — beat #{r.BeatNumber} {r.BeatId}");
        Console.WriteLine($"  {r.Book ?? "(no book)"} / {r.Chapter ?? "(no chapter)"} / universe {r.Universe ?? "?"}");
        Console.WriteLine($"  title: {r.BeatTitle ?? "(none)"}   text: {r.TextChars:N0} chars   version: {r.Version}   last write reason: {r.LastWriteReason ?? "(none)"}");
        Console.WriteLine(r.Complete
            ? $"  post-write cluster: COMPLETE (stage log written {r.WriteFinishedAt:u})"
            : r.Stages.Count > 0
                ? "  post-write cluster: INCOMPLETE — stage rows exist but no (complete) marker: the cluster threw or is still running"
                : "  post-write cluster: NO STAGE LOG — this beat was not written through the instrumented router (or the write is still in flight)");
        Console.WriteLine();

        Console.WriteLine($"LLM CALLS: {r.TotalCalls} total = {r.ChatCalls} chat + {r.EmbedCalls} embedding   failed: {r.FailedCalls}");
        Console.WriteLine($"  tokens in {r.InputTokens:N0} / out {r.OutputTokens:N0}   cost ${r.TotalCost:F4}" +
                          (r.WallMs is { } w ? $"   Σ stage wall time {w / 1000.0:F1}s" : ""));
        if (r.UnattributedCalls > 0)
            Console.WriteLine($"  !! {r.UnattributedCalls} call(s) UNATTRIBUTED (Stage = null) — a call path not inside any traced stage; fix the plumbing, don't ignore these.");
        if (r.TotalCalls == 0)
            Console.WriteLine("  COULD NOT LOOK — zero LlmCallHistory rows carry this beat id. Either the write predates the instrumentation or no call was tagged.");
        Console.WriteLine();

        if (r.CallsByStage.Count > 0)
        {
            Console.WriteLine("CALLS BY STAGE                                   calls  chat  embed  fail     in-tok   out-tok      cost    ms  models");
            foreach (var g in r.CallsByStage)
                Console.WriteLine($"  {Trunc(g.Stage, 46),-46} {g.Calls,5} {g.ChatCalls,5} {g.EmbedCalls,6} {g.Failed,5} {g.InputTokens,10:N0} {g.OutputTokens,9:N0} {g.Cost,9:F4} {g.ElapsedMs,6}  {g.Models}");
            Console.WriteLine();
        }

        if (r.Stages.Count > 0)
        {
            Console.WriteLine("STAGES (execution order)                              phase     ms  ok  llm      cost  coverage");
            foreach (var s in r.Stages)
            {
                var cov = s.CoverageApplicable == null ? "-"
                    : s.CoverageApplicable == false ? "skipped(gate)"
                    : s.CoverageActive == true ? $"active {s.CoverageChars:N0}ch"
                    : "applicable, empty";
                Console.WriteLine($"  {s.Ordinal,3} {Trunc(s.Stage, 50),-50} {s.Phase,-5} {s.ElapsedMs,6} {(s.Succeeded ? "ok" : "ERR"),3} {s.LlmCalls,4} {s.LlmCost,9:F4}  {cov}");
            }
            Console.WriteLine();
        }

        if (r.SkippedByGate.Count > 0)
        {
            Console.WriteLine("SKIPPED (gate false — coverage row says not applicable): " + string.Join(", ", r.SkippedByGate));
            Console.WriteLine();
        }

        Console.WriteLine("Every row above is a measurement of ONE write. Recommend nothing from a single beat; run several and compare.");
    }

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
