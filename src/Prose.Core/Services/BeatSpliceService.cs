using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>One beat's result in a splice run.</summary>
/// <param name="Status">planned · written · unchanged · conflict · error · verified · verify-miss.</param>
public sealed record SpliceBeatResult(int Beat, Guid BeatId, int Splices, int UnwrappedTags, string Status, string? Detail = null);

public sealed record SpliceReport(
    bool Applied,
    bool Aborted,
    int Beats,
    int Splices,
    IReadOnlyList<string> GuardFailures,
    IReadOnlyList<SpliceBeatResult> Results,
    int VerifyMisses);

/// <summary>
/// <c>prose --splice-beats</c> / MCP <c>splice_beats</c>: a hand-written docket of exact-text
/// replacements applied across a book, all-or-nothing at the guard.
///
/// <para>Promoted 2026-09-22 from a scratchpad <c>splice.py</c> that carried ~377 splices across 207
/// BCODA beats in one evening (Foundations doctrine: reuse twice → engine command). The script had
/// three costs this removes: it dumped the WHOLE book twice per docket (once to plan, once to verify —
/// the full read hung twenty minutes once), it spawned one CLI process per beat, and it wrote
/// tag-stripped text back, shedding pinned entity tags (see <see cref="BeatSplice"/>).</para>
///
/// <para><b>Contract.</b> Every edit's count is checked before anything is written; ONE mismatch
/// anywhere aborts the whole docket with nothing written. Dry-run unless <c>apply</c>. Each beat is
/// written once, through <see cref="NodeWorkbenchService.UpdateBeatTextAsync"/> as
/// <see cref="BeatWriteReason.AuthorEdit"/> (the docket is the author's own hand — RFC 0009), with the
/// <c>UpdatedAt</c> read at plan time as the conflict guard, so a beat another session touched since
/// is refused rather than overwritten. Then only the written beats are re-read and checked against the
/// text the splice promised.</para>
/// </summary>
public sealed class BeatSpliceService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    EditSessionService sessions)
{
    private static readonly JsonSerializerOptions DocketJson = new() { PropertyNameCaseInsensitive = true };

    private sealed record DocketRow(int Beat, string? Old, string? New, int? Count);

    /// <summary>Parse <c>[{beat, old, new, count?}]</c>. <c>new</c> may be empty or null (deletion).</summary>
    public static List<SpliceEdit> ParseDocket(string json)
    {
        var rows = JsonSerializer.Deserialize<List<DocketRow>>(json, DocketJson)
                   ?? throw new FormatException("Docket is empty.");
        return rows.Select(r => new SpliceEdit(r.Beat, r.Old ?? "", r.New ?? "", r.Count ?? 1)).ToList();
    }

    public async Task<SpliceReport> RunAsync(
        Guid nodeId, IReadOnlyList<SpliceEdit> docket, bool apply, bool deferAnalysis = true,
        CancellationToken ct = default)
    {
        var numbers = docket.Select(e => e.Beat).Distinct().ToList();

        // Membership comes from the node's reading order (so a beat from another book can never be
        // hit by a typo'd number); the prose itself is read only for the beats the docket names.
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);
        // DistinctBy: a beat shared by two nodes of the tree appears twice in the reading order.
        var inNode = ordered.Where(o => numbers.Contains(o.Beat.Number))
                            .DistinctBy(o => o.Beat.Id)
                            .ToDictionary(o => o.Beat.Number, o => o.Beat.Id);

        var failures = new List<string>();
        foreach (var n in numbers.Where(n => !inNode.ContainsKey(n)))
            failures.Add($"#{n}: not a beat of this node");

        var ids = inNode.Values.ToList();
        Dictionary<Guid, (string Text, DateTime UpdatedAt, int Version, string? Hash)> rows;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            rows = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => (b.Text ?? "", b.UpdatedAt, b.Version, b.TextHash), ct);
        }

        var plans = new List<(int Number, Guid Id, SpliceOutcome Outcome, int Splices)>();
        foreach (var group in docket.GroupBy(e => e.Beat))
        {
            if (!inNode.TryGetValue(group.Key, out var id)) continue;
            var outcome = BeatSplice.Apply(rows[id].Text, group);
            failures.AddRange(outcome.Failures);
            plans.Add((group.Key, id, outcome, group.Count()));
        }

        if (failures.Count > 0 || !apply)
        {
            var planned = plans.Select(p => new SpliceBeatResult(p.Number, p.Id, p.Splices, p.Outcome.UnwrappedTags, "planned")).ToList();
            return new SpliceReport(false, failures.Count > 0, plans.Count, docket.Count, failures, planned, 0);
        }

        var results = new List<SpliceBeatResult>();
        foreach (var p in plans)
        {
            var row = rows[p.Id];
            try
            {
                await workbench.UpdateBeatTextAsync(p.Id, p.Outcome.Text, BeatWriteReason.AuthorEdit,
                    expectedUpdatedAt: row.UpdatedAt, deferAnalysis: deferAnalysis, ct: ct, logEditSession: false);
                // Synchronous session log (in place of the workbench's fire-and-forget one, which a CLI exit drops).
                await sessions.TryLogBeatAsync(p.Id, row.Version, row.Hash, ct);
                results.Add(new SpliceBeatResult(p.Number, p.Id, p.Splices, p.Outcome.UnwrappedTags, "written"));
            }
            catch (BeatConflictException)
            {
                results.Add(new SpliceBeatResult(p.Number, p.Id, p.Splices, p.Outcome.UnwrappedTags, "conflict",
                    "changed by someone else since it was read — not written; re-run the docket"));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results.Add(new SpliceBeatResult(p.Number, p.Id, p.Splices, p.Outcome.UnwrappedTags, "error", ex.Message));
            }
        }

        // Read back ONLY what was written and compare with what the splice promised.
        var written = results.Where(r => r.Status == "written").Select(r => r.BeatId).ToList();
        Dictionary<Guid, string> live;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            live = await db.Beats.AsNoTracking().Where(b => written.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Text ?? "", ct);
        }

        var misses = 0;
        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            if (r.Status != "written") continue;
            var plan = plans.First(p => p.Id == r.BeatId);
            if (VerifyReadBack(plan.Outcome.ExpectedPlain, live.GetValueOrDefault(r.BeatId, "")) is { } miss)
            {
                misses++;
                results[i] = r with { Status = "verify-miss", Detail = miss };
            }
            else results[i] = r with { Status = "verified" };
        }

        return new SpliceReport(true, false, plans.Count, docket.Count, [], results, misses);
    }

    /// <summary>Null when the stored beat reads exactly as promised; otherwise where it first differs.
    /// The save trims and sanitizes, so the promise is put through the same door before comparing.</summary>
    public static string? VerifyReadBack(string expectedPlain, string liveStored)
    {
        var promised = BeatMarkup.StripEntityTags(TextSanitizerService.Sanitize((expectedPlain ?? "").Trim()));
        var readBack = BeatMarkup.StripEntityTags(liveStored);
        return readBack == promised ? null : FirstDifference(promised, readBack);
    }

    private static string FirstDifference(string expected, string actual)
    {
        var i = 0;
        while (i < expected.Length && i < actual.Length && expected[i] == actual[i]) i++;
        string At(string s) => BeatSplice.Preview(s[Math.Min(i, s.Length)..]);
        return $"differs at char {i}: expected {At(expected)} got {At(actual)}";
    }
}
