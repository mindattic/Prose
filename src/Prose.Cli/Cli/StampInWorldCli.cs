using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --stamp-in-world --slug &lt;book&gt; [--start &lt;ISO datetime&gt;] [--dry-run]</c>
///
/// <para>Writes <c>Beat.InWorldDate</c> for a book, so the GLMZ wiki can lay every book on one
/// cross-book timeline (PRS-T1069). Runs the same LLM pass as <c>--timeline</c>
/// (<see cref="BeatTimelineInference"/>), which gives each beat a story-relative "Day N, HH:MM",
/// then pins that relative clock to a calendar with one anchor instant: the in-world moment of
/// <b>Day 1, 00:00</b>.</para>
///
/// <para><b>The anchor.</b> <c>--start</c> when given (it IS Day 1, 00:00). Otherwise it is derived
/// from the earliest beat (in reading order) whose own prose states an absolute date (2100-2399)
/// and whose day/time is known: that date minus the beat's Day/time offset. A date stated only to
/// the year or month is filled with the 1st and flagged in the output. With neither, the command
/// refuses (exit 2) rather than invent a calendar.</para>
///
/// <para><b>What it writes.</b> For every beat whose day and time of day are both known:
/// <c>InWorldDate = anchor + (Day-1) days + time of day</c>. A beat with an unknown clock keeps
/// whatever InWorldDate it already had (null for every live book today). Only
/// <c>InWorldDate</c> is assigned: <c>ProseDbContext.StampBeatTextHash</c> only re-hashes a
/// Modified beat when its <c>Text</c> property is modified, and the write is classified as
/// BeatMetadata, not BeatText, so no beat is un-read, marked Stale or re-scored by this.
/// <see cref="Core.Data.Entities.Beat.StoryPosition"/> stays the authoritative order; this is the
/// clock overlay. Saved in batches of <see cref="BatchSize"/>.</para>
///
/// <para>Exit codes: 0 success (including a dry run); 1 bad usage, unknown book, or the inference
/// produced no rows; 2 no anchor (no <c>--start</c> and no absolute date in the prose).</para>
/// </summary>
public static class StampInWorldCli
{
    private const int BatchSize = 200;

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var slug = Flag(args, "--slug") ?? Flag(args, "--code") ?? Flag(args, "--id");
        var startArg = Flag(args, "--start");
        var dry = args.Contains("--dry-run") || args.Contains("--dry");

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("usage: prose --stamp-in-world --slug <book> [--start <ISO datetime>] [--dry-run]");
            return 1;
        }

        DateTime? start = null;
        if (startArg is not null)
        {
            if (!DateTime.TryParse(startArg, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var parsed))
            {
                Console.Error.WriteLine($"[stamp-in-world] --start '{startArg}' is not an ISO date/time (e.g. 2226-03-14 or 2226-03-14T06:00).");
                return 1;
            }
            start = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        }

        var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await NodeRefResolver.ResolveNodeAsync(db, slug);
        if (node is null)
        {
            Console.Error.WriteLine($"[stamp-in-world] No node matched '{slug}'.");
            return 1;
        }

        Console.WriteLine($"[stamp-in-world] {node.Title}  ({node.Slug})");
        var llm = sp.GetRequiredService<ILlmService>();
        var result = await BeatTimelineInference.InferAsync(db, llm, node, Console.Out, Console.Error);
        if (result.Error is not null)
        {
            Console.Error.WriteLine($"[stamp-in-world] {result.Error}");
            return 1;
        }

        // A beat linked under two chapters appears twice; it has one column, so its first
        // occurrence in reading order wins (same rule as BeatStoryPositionService).
        var rows = result.Rows.DistinctBy(r => r.BeatId).ToList();
        var known = rows.Where(r => r.HasClock).ToList();
        var dated = rows.Where(r => r.AbsoluteDate.HasValue).ToList();
        var conflicts = rows.Count(r => r.IsConflict);

        // ── Anchor: the in-world instant of Day 1, 00:00 ─────────────────────────
        DateTime anchor;
        string anchorSource;
        if (start.HasValue)
        {
            anchor = start.Value;
            anchorSource = "--start";
        }
        else
        {
            var basis = dated.FirstOrDefault(r => r.HasClock);
            if (basis is null)
            {
                Console.Error.WriteLine(
                    $"[stamp-in-world] Refusing: no anchor for '{node.Slug}'. No --start was given, and " +
                    (dated.Count == 0
                        ? "no beat's prose states an absolute date (year 2100-2399)."
                        : $"the {dated.Count} beat(s) whose prose states an absolute date have no known day/time to offset from.") +
                    " Pass --start <ISO datetime> for the in-world instant of Day 1, 00:00.");
                return 2;
            }

            // A date-only (or coarser) statement fixes the calendar day, not the clock, so subtract
            // whole days; a date with a stated time pins the exact minute.
            anchor = basis.AbsoluteDatePrecision == "minute"
                ? basis.AbsoluteDate!.Value.AddMinutes(-basis.OffsetMinutes!.Value)
                : basis.AbsoluteDate!.Value.Date.AddDays(-(basis.DayIndex!.Value - 1));
            anchorSource = $"beat #{basis.Number} (position {basis.StoryPosition?.ToString() ?? "?"}) states " +
                           $"{TimelineCli.FormatDate(basis.AbsoluteDate.Value, basis.AbsoluteDatePrecision)}" +
                           (basis.DateAnchor is not null ? $" (\"{basis.DateAnchor}\")" : "") +
                           $" at {basis.AbsoluteTime}";
            if (basis.AbsoluteDatePrecision is "year" or "month")
                Console.WriteLine($"[stamp-in-world] WARNING: the anchor date is only stated to the {basis.AbsoluteDatePrecision}; " +
                                  "the missing parts were filled with the 1st. Pass --start to pin it exactly.");
        }

        // ── Compute and (optionally) write ────────────────────────────────────────
        var targets = known.ToDictionary(r => r.BeatId, r => anchor.AddMinutes(r.OffsetMinutes!.Value));

        if (dry)
        {
            Console.WriteLine();
            Console.WriteLine($"  {"pos",6} {"#",7}  {"Story clock",-24} {"InWorldDate",-17} Chapter");
            Console.WriteLine(new string('─', 92));
            foreach (var r in rows)
            {
                var when = targets.TryGetValue(r.BeatId, out var t) ? t.ToString("yyyy-MM-dd HH:mm") : "(unknown)";
                Console.WriteLine($"  {r.StoryPosition?.ToString() ?? "?",6} {r.Number,7}  {r.AbsoluteTime ?? "(unknown)",-24} {when,-17} {r.ChapterTitle}" +
                                  (r.IsConflict ? "  ⚡" : ""));
            }
            Console.WriteLine(new string('─', 92));
        }

        int written = 0;
        if (!dry && targets.Count > 0)
        {
            var ids = targets.Keys.ToList();
            for (int i = 0; i < ids.Count; i += BatchSize)
            {
                var batch = ids.Skip(i).Take(BatchSize).ToList();
                await using var wdb = await dbFactory.CreateDbContextAsync();
                var beats = await wdb.Beats.Where(b => batch.Contains(b.Id)).ToListAsync();
                foreach (var b in beats)
                {
                    var target = targets[b.Id];
                    if (b.InWorldDate == target) continue;
                    // InWorldDate only: never Text/TextHash/UpdatedAt/Stale — see the class remarks.
                    b.InWorldDate = target;
                    written++;
                }
                await wdb.SaveChangesAsync();
            }
        }

        // ── Summary ─────────────────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine($"  Anchor (Day 1, 00:00): {anchor:yyyy-MM-dd HH:mm}  ← {anchorSource}");
        if (targets.Count > 0)
        {
            var first = targets.Values.Min();
            var last = targets.Values.Max();
            Console.WriteLine($"  Span: {first:yyyy-MM-dd HH:mm} → {last:yyyy-MM-dd HH:mm}  " +
                              $"(~{BeatTimelineInference.FormatElapsed((int)(last - first).TotalMinutes)})");
        }
        Console.WriteLine($"  Beats: {rows.Count}  ·  Stamped: {targets.Count}{(dry ? " (dry run)" : $" ({written} changed)")}" +
                          $"  ·  Unknown: {rows.Count - known.Count}  ·  Conflicts: {conflicts}  ·  Dated in prose: {dated.Count}");
        if (result.FailedChunks > 0)
            Console.WriteLine($"  {result.FailedChunks} of {result.Chunks} chunk(s) failed; their beats were left unchanged.");
        if (dry) Console.WriteLine("[stamp-in-world] Dry run — nothing written.");
        return 0;
    }

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
