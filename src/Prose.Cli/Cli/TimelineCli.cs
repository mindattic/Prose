using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --timeline (--slug slug | --id id)</c>
///   Scans every beat in a node (or every episode in a book) for clock and day
///   references, infers story-relative timestamps, and prints an elapsed-time table.
///   Flags continuity conflicts where stated time contradicts the prior sequence.
///
///   ● = explicit time anchor extracted from prose
///   ⚡ = time conflict detected
///   (blank) = time carried forward from prior beat
///
///   For books the timeline is stitched episode-by-episode, keeping elapsed time
///   continuous across episode boundaries. The inference itself lives in
///   <see cref="BeatTimelineInference"/> (shared with <c>--stamp-in-world</c>); this command
///   only prints it and writes nothing.
/// </summary>
public static class TimelineCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        string? slug = null, id = null, code = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--id":   if (i + 1 < args.Length) id   = args[++i]; break;
                case "--code": if (i + 1 < args.Length) code = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(code))
        {
            Console.Error.WriteLine("usage: prose --timeline (--slug <slug> | --id <id> | --code <code>)");
            return 1;
        }

        var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // The shared resolver for code, slug and id alike. The private --id branch applied the
        // ambient universe filter and took the FIRST node whose id started with the prefix, so an
        // ambiguous prefix silently timelined an arbitrary book.
        var reference = !string.IsNullOrWhiteSpace(code) ? code : !string.IsNullOrWhiteSpace(slug) ? slug : id;
        Core.Data.Entities.Node? node = await NodeRefResolver.ResolveNodeAsync(db, reference);

        if (node is null)
        {
            Console.Error.WriteLine("[timeline] node not found");
            return 1;
        }

        Console.WriteLine($"[timeline] {node.Title}  ({node.Slug})");

        var llm = sp.GetRequiredService<ILlmService>();
        var result = await BeatTimelineInference.InferAsync(db, llm, node, Console.Out, Console.Error);
        if (result.Error is not null)
        {
            Console.Error.WriteLine($"[timeline] {result.Error}");
            return 1;
        }

        // Print the combined table.
        Console.WriteLine();
        Console.WriteLine($"=== Timeline: {node.Title} ===");
        Console.WriteLine();
        Console.WriteLine($"  {"":2} {"sk",6}  {"Time",-24} {"Elapsed",-12} Beat");
        Console.WriteLine(new string('─', 92));

        int conflicts = 0, anchored = 0;
        string? currentEp = null;
        int? lastElapsed = null;

        foreach (var row in result.Rows)
        {
            // Episode header when we enter a new one.
            if (row.ChapterTitle != currentEp)
            {
                currentEp = row.ChapterTitle;
                if (currentEp is not null)
                    Console.WriteLine($"  ── {currentEp} ──");
            }

            var preview = row.Text.Trim().Replace('\n', ' ').Replace('\r', ' ');
            if (preview.Length > 55) preview = preview[..55] + "…";

            var marker  = row.IsConflict ? "⚡" : row.IsAnchored ? "●" : " ";
            var timeStr = row.AbsoluteTime ?? "(unknown)";
            var elapsed = row.ElapsedMinutes.HasValue ? BeatTimelineInference.FormatElapsed(row.ElapsedMinutes.Value) : "?";

            if (row.IsAnchored) anchored++;
            if (row.ElapsedMinutes.HasValue) lastElapsed = row.ElapsedMinutes;

            Console.WriteLine($"  {marker,2} {row.SortKey,6:F0}  {timeStr,-24} {elapsed,-12} {preview}");

            if (row.IsConflict)
            {
                Console.WriteLine($"         ↳ CONFLICT: {row.ConflictNote}");
                conflicts++;
            }
            if (row.AbsoluteDate.HasValue)
                Console.WriteLine($"         ↳ DATE: {FormatDate(row.AbsoluteDate.Value, row.AbsoluteDatePrecision)}" +
                                  (row.DateAnchor is not null ? $"  (\"{row.DateAnchor}\")" : ""));
        }

        Console.WriteLine(new string('─', 92));
        Console.WriteLine();
        if (lastElapsed.HasValue)
            Console.WriteLine($"  Total story span:  ~{BeatTimelineInference.FormatElapsed(lastElapsed.Value)}");
        Console.WriteLine($"  Beats: {result.Rows.Count}  ·  Time-anchored: {anchored}  ·  Conflicts: {conflicts}");
        Console.WriteLine();

        return 0;
    }

    internal static string FormatDate(DateTime date, string? precision) => precision switch
    {
        "year"  => date.ToString("yyyy"),
        "month" => date.ToString("yyyy-MM"),
        "day"   => date.ToString("yyyy-MM-dd"),
        _       => date.ToString("yyyy-MM-dd HH:mm"),
    };
}
