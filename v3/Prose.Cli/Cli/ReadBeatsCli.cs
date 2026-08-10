using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --read-beats --slug &lt;slug&gt; (--from &lt;N&gt; --to &lt;N&gt; | --numbers &lt;csv&gt;)</c>
///
/// Prints beat text WITH its authoritative POV character attached, sourced fresh from
/// <c>BeatEntityPresence</c> (PresenceType='pov') every call — never from inference.
///
/// This exists because of a real, live mistake (2026-08-10, VIGL): reading a run of beats'
/// raw <c>Beats.Text</c> via ad-hoc SQL and inferring which character a passage belonged to
/// from prose content alone, across a multi-POV book, misattributed a scribe sister's
/// characterization to her knight sister for several beats in a row — a wrong conclusion that
/// very nearly informed changes to the wrong character's voice. The author's own correction:
/// canon data must be checked BEFORE forming conclusions from prose, not after, and that check
/// has to live in the tool, not in a memory file a future session might not reread. This
/// command makes that check the only way to read a beat's text through this CLI at all — POV
/// is printed alongside the text unconditionally, so misattribution requires ignoring a visible
/// label, not just missing an absent one.
///
/// If a book has NO PresenceType='pov' rows at all (still true for most of the corpus — see
/// BookHealthService.SacredFlawAsync's own "no-pov-data" finding), this prints a loud warning
/// instead of silently omitting the attribution, so the absence of ground truth is as visible
/// as its presence would have been.
/// </summary>
public static class ReadBeatsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var slug = Flag(args, "--slug");
        var fromStr = Flag(args, "--from");
        var toStr = Flag(args, "--to");
        var numbersCsv = Flag(args, "--numbers");

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --read-beats --slug <slug> (--from <N> --to <N> | --numbers <csv>)");
            return 2;
        }

        List<int>? explicitNumbers = null;
        int? from = null, to = null;
        if (!string.IsNullOrWhiteSpace(numbersCsv))
        {
            explicitNumbers = numbersCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse).ToList();
        }
        else if (int.TryParse(fromStr, out var f) && int.TryParse(toStr, out var t))
        {
            from = f; to = t;
        }
        else
        {
            Console.Error.WriteLine("Usage: prose --read-beats --slug <slug> (--from <N> --to <N> | --numbers <csv>)");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .Where(n => n.Slug == slug || n.NodeCode == slug)
            .Select(n => new { n.Id, n.Title })
            .FirstOrDefaultAsync();
        if (node == null) { Console.Error.WriteLine($"Node not found: {slug}"); return 1; }

        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);

        var beatQuery =
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            join c in db.Nodes.AsNoTracking() on bn.NodeId equals c.Id
            where leafIds.Contains(bn.NodeId) && bn.IsEnabled
            select new { b.Id, b.Number, b.Text, ChapterTitle = c.Title };

        if (explicitNumbers != null)
            beatQuery = beatQuery.Where(x => explicitNumbers.Contains(x.Number));
        else
            beatQuery = beatQuery.Where(x => x.Number >= from!.Value && x.Number <= to!.Value);

        var beats = await beatQuery.OrderBy(x => x.Number).ToListAsync();
        if (beats.Count == 0) { Console.WriteLine("No matching beats found."); return 0; }

        var beatIds = beats.Select(x => x.Id).ToList();
        var povByBeat = new Dictionary<Guid, string>();
        if (beatIds.Count > 0)
        {
            var beatParams = beatIds.Select((id, i) => new SqlParameter($"@b{i}", id)).ToArray();
            var placeholders = string.Join(",", beatParams.Select(p => p.ParameterName));
            var povRows = await db.Database.SqlQueryRaw<PovRow>(
                "SELECT BeatId, EntityName FROM BeatEntityPresence " +
                $"WHERE PresenceType = 'pov' AND BeatId IN ({placeholders})",
                beatParams).ToListAsync();
            foreach (var r in povRows) povByBeat[r.BeatId] = r.EntityName;
        }

        var anyPovData = povByBeat.Count > 0;
        Console.WriteLine($"=== {node.Title} ({slug}) — {beats.Count} beat(s) ===");
        if (!anyPovData)
        {
            Console.WriteLine();
            Console.WriteLine("!! NO BeatEntityPresence PresenceType='pov' rows for any beat in this range. !!");
            Console.WriteLine("!! Do NOT infer character attribution from prose content alone in a multi-POV  !!");
            Console.WriteLine("!! book -- check Nodes.NodeBible's POV Map, or ask, before drawing conclusions.!!");
        }
        Console.WriteLine();

        foreach (var b in beats)
        {
            var pov = povByBeat.TryGetValue(b.Id, out var name) ? name : "*** POV UNKNOWN -- no BeatEntityPresence row ***";
            Console.WriteLine($"--- Beat #{b.Number} [POV: {pov}] ({b.ChapterTitle}) ---");
            Console.WriteLine(b.Text);
            Console.WriteLine();
        }

        return 0;
    }

    private record PovRow(Guid BeatId, string EntityName);

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
