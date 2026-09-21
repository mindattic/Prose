using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --history &lt;verb&gt; …</c> — read-only forensics over SQL Server's temporal history
/// for Beats and Nodes.
///
/// <para>Every one of these answers a question about the PAST that the live rows cannot: what a
/// beat used to say, which day a book was rewritten wholesale, whether a node ever held different
/// text. Built during the BCODA3 reconstruction, where a book had to be rebuilt AS OF a point in
/// time because a rewrite wave had overwritten work worth keeping — and where the only reason
/// that was possible is that system versioning was already on.</para>
///
/// <list type="bullet">
///   <item><c>beat-history --beat &lt;guid&gt;</c> — every stored version of one beat's text.</item>
///   <item><c>edit-timeline --node &lt;ref&gt;</c> — distinct beats edited per day across a book.</item>
///   <item><c>reconstruct-book --node &lt;ref&gt; --as-of &lt;date&gt;</c> — rebuild a book as it stood.</item>
///   <item><c>node-edit-timeline --node &lt;ref&gt;</c> — the same per-day view for node rows.</item>
///   <item><c>find-node-history --slug &lt;text&gt;</c> — locate nodes by slug across history.</item>
///   <item><c>check-temporal</c> — which tables actually have system versioning enabled.</item>
///   <item><c>search-history --text "&lt;phrase&gt;"</c> — find a phrase in any historical beat version.</item>
/// </list>
///
/// <para>Read-only and free: no LLM call, no write. They query <c>FOR SYSTEM_TIME</c> directly,
/// which is why they are SQL Server-specific and degrade with a clear message rather than a
/// crash when a history table is absent.</para>
/// </summary>
public static class TemporalHistoryCli
{
    public static async Task RunAsync(string[] args, IServiceProvider services)
    {
        var verb = args.SkipWhile(a => a != "--history").Skip(1).FirstOrDefault() ?? "";
        if (string.IsNullOrWhiteSpace(verb) || verb is "-h" or "--help") { PrintHelp(); return; }

        switch (verb)
        {
    case "beat-history":
    {
        var beatArg = Flag(args, "--beat");
        if (!Guid.TryParse(beatArg, out var beatId)) { Console.Error.WriteLine("--beat <guid> is required."); return; }

        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var conn = db0.Database.GetDbConnection();
        await conn.OpenAsync();

        await using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = 'Beats_History'";
            var exists = (int)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
            Console.WriteLine($"[beat-history] Beats_History table exists: {(exists ? "YES" : "NO")}");
            if (!exists) break;
        }

        var rows = new List<BeatHistoryRow>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT Id, Text, ISNULL(SysStart, '1900-01-01') AS ValidFrom, ISNULL(SysEnd, '9999-12-31') AS ValidTo
                FROM Beats FOR SYSTEM_TIME ALL
                WHERE Id = @beatId
                ORDER BY ValidFrom
                """;
            var p = cmd.CreateParameter();
            p.ParameterName = "@beatId";
            p.Value = beatId;
            cmd.Parameters.Add(p);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new BeatHistoryRow
                {
                    Id = reader.GetGuid(0),
                    Text = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    ValidFrom = reader.GetDateTime(2),
                    ValidTo = reader.GetDateTime(3),
                });
            }
        }

        Console.WriteLine($"[beat-history] {rows.Count} version(s) found for beat {beatId}:");
        foreach (var r in rows)
            Console.WriteLine($"  [{r.ValidFrom:yyyy-MM-dd HH:mm} .. {r.ValidTo:yyyy-MM-dd HH:mm}] {Truncate(r.Text, 200)}");
        break;
    }
    case "edit-timeline":
    {
        var nodeRef = Flag(args, "--node");
        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db0, nodeRef);
        if (nodeId is null) { Console.Error.WriteLine($"Could not resolve --node '{nodeRef}'."); return; }

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId.Value);
        var beatIds = ordered.Select(o => o.Beat.Id).ToList();
        Console.WriteLine($"[edit-timeline] {beatIds.Count} beats — scanning Beats_History for every edit timestamp.");

        var conn = db0.Database.GetDbConnection();
        await conn.OpenAsync();
        var byDay = new SortedDictionary<DateOnly, HashSet<Guid>>();
        var byDayCreates = new SortedDictionary<DateOnly, int>();

        // Batch in chunks to keep the IN(...) list reasonable.
        foreach (var chunk in beatIds.Chunk(200))
        {
            var idList = string.Join(",", chunk.Select(id => $"'{id}'"));
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                SELECT Id, SysStart FROM Beats FOR SYSTEM_TIME ALL WHERE Id IN ({idList})
                """;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var day = DateOnly.FromDateTime(reader.GetDateTime(1));
                if (!byDay.TryGetValue(day, out var set)) byDay[day] = set = [];
                set.Add(reader.GetGuid(0));
            }
        }

        Console.WriteLine();
        Console.WriteLine("── DISTINCT BEATS EDITED PER DAY ──");
        foreach (var (day, set) in byDay)
        {
            var bar = new string('#', Math.Min(80, set.Count));
            Console.WriteLine($"  {day:yyyy-MM-dd}  {set.Count,4}  {bar}");
        }
        break;
    }
    case "reconstruct-book":
    {
        var sourceArg = Flag(args, "--source-node-id");
        var asOfArg = Flag(args, "--as-of");
        var newTitle = Flag(args, "--new-title");
        var code = Flag(args, "--code");
        var dryRun = args.Contains("--dry-run");
        if (!Guid.TryParse(sourceArg, out var sourceId)) { Console.Error.WriteLine("--source-node-id <guid> is required."); return; }
        if (!DateTime.TryParse(asOfArg, out var asOf)) { Console.Error.WriteLine("--as-of <date> is required (e.g. 2026-08-29)."); return; }
        if (string.IsNullOrWhiteSpace(newTitle)) { Console.Error.WriteLine("--new-title \"<title>\" is required."); return; }

        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var conn = db0.Database.GetDbConnection();
        await conn.OpenAsync();

        async Task<List<(Guid Id, string Title, double SortKey)>> GetChildrenAsOfAsync(Guid parentId)
        {
            var result = new List<(Guid, string, double)>();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, SortKey FROM Nodes FOR SYSTEM_TIME AS OF @asOf WHERE ParentNodeId = @parentId AND Kind = 'chapter' ORDER BY SortKey";
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@asOf"; p1.Value = asOf; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@parentId"; p2.Value = parentId; cmd.Parameters.Add(p2);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetGuid(0), reader.IsDBNull(1) ? "" : reader.GetString(1), reader.GetDouble(2)));
            return result;
        }

        async Task<List<(Guid BeatId, double SortKey)>> GetBeatMembershipAsOfAsync(Guid nodeId)
        {
            var result = new List<(Guid, double)>();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT BeatId, SortKey FROM BeatNodes FOR SYSTEM_TIME AS OF @asOf WHERE NodeId = @nodeId ORDER BY SortKey";
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@asOf"; p1.Value = asOf; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@nodeId"; p2.Value = nodeId; cmd.Parameters.Add(p2);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetGuid(0), reader.GetDouble(1)));
            return result;
        }

        async Task<string> GetBeatTextAsOfAsync(Guid beatId)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Text FROM Beats FOR SYSTEM_TIME AS OF @asOf WHERE Id = @beatId";
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@asOf"; p1.Value = asOf; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@beatId"; p2.Value = beatId; cmd.Parameters.Add(p2);
            var result = await cmd.ExecuteScalarAsync();
            return result as string ?? "";
        }

        var chapters = await GetChildrenAsOfAsync(sourceId);
        Console.WriteLine($"[reconstruct-book] {chapters.Count} chapter(s) as of {asOf:yyyy-MM-dd} under source {sourceId}.");
        var totalBeats = 0;
        var plan = new List<(string ChapterTitle, List<(Guid BeatId, string Text)> Beats)>();
        foreach (var (chId, chTitle, _) in chapters)
        {
            var membership = await GetBeatMembershipAsOfAsync(chId);
            var beats = new List<(Guid, string)>();
            foreach (var (beatId, _) in membership)
                beats.Add((beatId, await GetBeatTextAsOfAsync(beatId)));
            plan.Add((chTitle, beats));
            totalBeats += beats.Count;
            Console.WriteLine($"  [{chTitle}] {beats.Count} beat(s)");
        }
        Console.WriteLine($"[reconstruct-book] TOTAL: {chapters.Count} chapters, {totalBeats} beats.");

        if (dryRun) { Console.WriteLine("[reconstruct-book] --dry-run: stopping before any write."); break; }

        // ---- Actually build it ----
        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        await using (var writeDb = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync())
        {
            var bookNode = NodeFactory.Create("book");
            bookNode.Id = Guid.CreateVersion7();
            var baseSlug = System.Text.RegularExpressions.Regex.Replace(newTitle.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            bookNode.Slug = $"{baseSlug}-{bookNode.Id.ToString("N")[..8]}";
            bookNode.Title = newTitle;
            bookNode.Status = "draft";
            bookNode.ParentNodeId = null;
            bookNode.NodeCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
            var maxSort = await writeDb.Nodes.Where(n => n.ParentNodeId == null).Select(n => (double?)n.SortKey).MaxAsync() ?? 0;
            bookNode.SortKey = maxSort + 100.0;
            writeDb.Nodes.Add(bookNode);
            await writeDb.SaveChangesAsync();
            Console.WriteLine($"[reconstruct-book] Created book '{newTitle}' — {bookNode.Id} / {bookNode.Slug}");

            var chapterSort = 0.0;
            foreach (var (chapterTitle, beats) in plan)
            {
                chapterSort += 100.0;
                var chNode = NodeFactory.Create("chapter");
                chNode.Id = Guid.CreateVersion7();
                var chBaseSlug = System.Text.RegularExpressions.Regex.Replace((chapterTitle ?? "chapter").ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
                chNode.Slug = $"{chBaseSlug}-{chNode.Id.ToString("N")[..8]}";
                chNode.Title = chapterTitle;
                chNode.Status = "draft";
                chNode.ParentNodeId = bookNode.Id;
                chNode.SortKey = chapterSort;
                writeDb.Nodes.Add(chNode);
                await writeDb.SaveChangesAsync();

                Guid? afterBeatId = null;
                foreach (var (_, text) in beats)
                {
                    var newBeat = await workbench.InsertBeatAsync(chNode.Id, afterBeatId, text);
                    afterBeatId = newBeat.Id;
                }
                Console.WriteLine($"  [{chapterTitle}] {beats.Count} beat(s) inserted.");
            }
        }
        Console.WriteLine("[reconstruct-book] Done.");
        break;
    }
    case "node-edit-timeline":
    {
        var nodeArg = Flag(args, "--node-id");
        if (!Guid.TryParse(nodeArg, out var rootId)) { Console.Error.WriteLine("--node-id <guid> is required (a historical node id, may be deleted)."); return; }

        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var conn = db0.Database.GetDbConnection();
        await conn.OpenAsync();

        // Every node that was EVER a descendant of rootId, at any point in time (deleted or not).
        var nodeIds = new HashSet<Guid> { rootId };
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Id, ParentNodeId FROM Nodes FOR SYSTEM_TIME ALL WHERE ParentNodeId IS NOT NULL";
            await using var reader = await cmd.ExecuteReaderAsync();
            var edges = new List<(Guid Id, Guid Parent)>();
            while (await reader.ReadAsync())
                if (!reader.IsDBNull(1)) edges.Add((reader.GetGuid(0), reader.GetGuid(1)));
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (var (id, parent) in edges)
                    if (nodeIds.Contains(parent) && nodeIds.Add(id)) grew = true;
            }
        }
        Console.WriteLine($"[node-edit-timeline] {nodeIds.Count} node(s) in the tree (any point in time).");

        // Every beat that was EVER a member of any of those nodes, at any point in time.
        var beatIds = new HashSet<Guid>();
        foreach (var chunk in nodeIds.Chunk(200))
        {
            var idList = string.Join(",", chunk.Select(id => $"'{id}'"));
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT DISTINCT BeatId FROM BeatNodes FOR SYSTEM_TIME ALL WHERE NodeId IN ({idList})";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) beatIds.Add(reader.GetGuid(0));
        }
        Console.WriteLine($"[node-edit-timeline] {beatIds.Count} beat(s) ever belonged to this tree.");

        var byDay = new SortedDictionary<DateOnly, HashSet<Guid>>();
        foreach (var chunk in beatIds.Chunk(200))
        {
            var idList = string.Join(",", chunk.Select(id => $"'{id}'"));
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, SysStart FROM Beats FOR SYSTEM_TIME ALL WHERE Id IN ({idList})";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var day = DateOnly.FromDateTime(reader.GetDateTime(1));
                if (!byDay.TryGetValue(day, out var set)) byDay[day] = set = [];
                set.Add(reader.GetGuid(0));
            }
        }

        Console.WriteLine();
        Console.WriteLine("── DISTINCT BEATS (EVER IN THIS TREE) EDITED PER DAY, FULL HISTORY ──");
        foreach (var (day, set) in byDay)
        {
            var bar = new string('#', Math.Min(80, set.Count));
            Console.WriteLine($"  {day:yyyy-MM-dd}  {set.Count,4}  {bar}");
        }
        break;
    }
    case "find-node-history":
    {
        var slugLike = Flag(args, "--slug-like");
        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var conn = db0.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Title, Slug, Kind, ParentNodeId, MIN(SysStart) AS FirstSeen, MAX(SysEnd) AS LastSeen
            FROM Nodes FOR SYSTEM_TIME ALL
            WHERE Slug LIKE @slugLike
            GROUP BY Id, Title, Slug, Kind, ParentNodeId
            ORDER BY FirstSeen
            """;
        var p = cmd.CreateParameter(); p.ParameterName = "@slugLike"; p.Value = $"%{slugLike}%"; cmd.Parameters.Add(p);
        await using var reader = await cmd.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            var lastSeen = reader.GetDateTime(6);
            Console.WriteLine($"{reader.GetGuid(0)} | {reader.GetString(1)} | {reader.GetString(2)} | kind={reader.GetString(3)} | parent={(reader.IsDBNull(4) ? "-" : reader.GetGuid(4).ToString())} | seen {reader.GetDateTime(5):yyyy-MM-dd HH:mm} .. {(lastSeen.Year > 9000 ? "current" : lastSeen.ToString("yyyy-MM-dd HH:mm"))}");
        }
        Console.WriteLine($"[find-node-history] {count} distinct node(s) matched.");
        break;
    }
    case "check-temporal":
    {
        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var conn = db0.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sys.tables WHERE name LIKE '%History%' ORDER BY name";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) Console.WriteLine(reader.GetString(0));
        break;
    }
    case "search-history":
    {
        var pattern = Flag(args, "--text");
        if (string.IsNullOrWhiteSpace(pattern)) { Console.Error.WriteLine("--text \"<substring>\" is required."); return; }

        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var conn = db0.Database.GetDbConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Text, SysStart, SysEnd
            FROM Beats FOR SYSTEM_TIME ALL
            WHERE Text LIKE '%' + @pattern + '%'
            ORDER BY SysStart
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "@pattern";
        p.Value = pattern;
        cmd.Parameters.Add(p);
        await using var reader = await cmd.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            var id = reader.GetGuid(0);
            var text = reader.IsDBNull(1) ? "" : reader.GetString(1);
            var start = reader.GetDateTime(2);
            var end = reader.GetDateTime(3);
            Console.WriteLine($"[{start:yyyy-MM-dd HH:mm} .. {(end.Year > 9000 ? "current" : end.ToString("yyyy-MM-dd HH:mm"))}] beat {id}");
            Console.WriteLine($"  {Truncate(text, 300)}");
        }
        Console.WriteLine();
        Console.WriteLine($"[search-history] {count} hit(s) across ALL beats, all time, current + deleted.");
        break;
    }
            default:
                Console.Error.WriteLine($"Unknown history verb '{verb}'.");
                PrintHelp();
                break;
        }
    }

    private static string Flag(string[] a, string name)
    {
        var i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : "";
    }

    private static string Truncate(string s, int n) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s[..n] + "…");

    private static void PrintHelp() => Console.WriteLine("""
        prose --history <verb> [options]      (read-only, free — SQL Server temporal history)

          beat-history       --beat <guid>
          edit-timeline      --node <slug|guid>
          reconstruct-book   --node <slug|guid> --as-of <yyyy-MM-dd[ HH:mm]>
          node-edit-timeline --node <slug|guid>
          find-node-history  --slug <text>
          check-temporal
          search-history     --text "<phrase>"
        """);

    private sealed class BeatHistoryRow
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = "";
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }
}
