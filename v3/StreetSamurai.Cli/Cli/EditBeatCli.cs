using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --edit-beat</c> — overwrite one beat's prose, or insert a new beat after a given position.
///
/// Edit mode (default):
///   --slug &lt;slug&gt;           Node slug.
///   --beat-number &lt;N&gt;       1-indexed beat position in reading order.
///   --file &lt;path&gt;           Path to a text file whose contents replace the beat prose.
///
/// Insert mode (--insert-after):
///   --slug &lt;slug&gt;           Node slug.
///   --insert-after &lt;N&gt;      Insert a new beat after position N (0 = insert at top).
///   --file &lt;path&gt;           Path to a text file whose contents become the new beat prose.
///
/// Exit codes: 0 = success, 1 = bad args / node not found / beat not found.
/// </summary>
public static class EditBeatCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, filePath = null;
        int beatNumber = 0, insertAfter = -1;
        bool insertMode = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":         if (i + 1 < args.Length) slug        = args[++i]; break;
                case "--beat-number":  if (i + 1 < args.Length) int.TryParse(args[++i], out beatNumber); break;
                case "--insert-after": if (i + 1 < args.Length) { insertMode = true; int.TryParse(args[++i], out insertAfter); } break;
                case "--file":         if (i + 1 < args.Length) filePath    = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[edit-beat] --slug is required.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("[edit-beat] --file is required.");
            return 1;
        }
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"[edit-beat] File not found: {filePath}");
            return 1;
        }
        if (!insertMode && beatNumber < 1)
        {
            Console.Error.WriteLine("[edit-beat] --beat-number must be ≥1, or use --insert-after.");
            return 1;
        }

        var prose = (await File.ReadAllTextAsync(filePath)).Trim();
        if (string.IsNullOrWhiteSpace(prose))
        {
            Console.Error.WriteLine("[edit-beat] Prose file is empty.");
            return 1;
        }

        var dbFactory  = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench  = services.GetRequiredService<NodeWorkbenchService>();

        // Resolve node
        Guid nodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug);
            if (node == null) { Console.Error.WriteLine($"[edit-beat] Node '{slug}' not found."); return 1; }
            nodeId = node.Id;
        }

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);

        if (insertMode)
        {
            Guid? afterId = null;
            if (insertAfter > 0)
            {
                if (insertAfter > ordered.Count)
                {
                    Console.Error.WriteLine($"[edit-beat] --insert-after {insertAfter} exceeds beat count ({ordered.Count}).");
                    return 1;
                }
                afterId = ordered[insertAfter - 1].Beat.Id;
            }
            var newBeat = await workbench.InsertBeatAsync(nodeId, afterId, prose);
            Console.WriteLine($"[edit-beat] Inserted new beat after position {insertAfter} → id {newBeat.Id} ({prose.Length} chars).");
            return 0;
        }

        if (beatNumber > ordered.Count)
        {
            Console.Error.WriteLine($"[edit-beat] --beat-number {beatNumber} exceeds beat count ({ordered.Count}).");
            return 1;
        }

        var target = ordered[beatNumber - 1].Beat;
        Console.Write($"[edit-beat] Updating beat #{beatNumber} (id {target.Id})… ");
        await workbench.UpdateBeatTextAsync(target.Id, prose, expectedUpdatedAt: null);
        Console.WriteLine($"ok ({prose.Length} chars).");
        return 0;
    }
}
