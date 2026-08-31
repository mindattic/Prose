using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --edit-beat</c> — overwrite one beat's prose, or insert a new beat after a given position.
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
        string? slug = null, filePath = null, idStr = null;
        int beatNumber = 0, insertAfter = -1;
        bool insertMode = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":         if (i + 1 < args.Length) slug        = args[++i]; break;
                case "--id":           if (i + 1 < args.Length) idStr       = args[++i]; break;
                case "--beat-number":  if (i + 1 < args.Length) int.TryParse(args[++i], out beatNumber); break;
                case "--insert-after": if (i + 1 < args.Length) { insertMode = true; int.TryParse(args[++i], out insertAfter); } break;
                case "--file":         if (i + 1 < args.Length) filePath    = args[++i]; break;
            }
        }

        // ── Edit-by-id mode: splice one beat by its exact GUID (position/node agnostic).
        // Routes through the same workbench path so the edit is logged to the open EditSession.
        if (!string.IsNullOrWhiteSpace(idStr))
        {
            if (!Guid.TryParse(idStr, out var beatId))
            {
                Console.Error.WriteLine($"[edit-beat] --id is not a valid GUID: {idStr}");
                return 1;
            }
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.Error.WriteLine("[edit-beat] --file is required and must exist.");
                return 1;
            }
            var proseById = (await File.ReadAllTextAsync(filePath)).Trim();
            if (string.IsNullOrWhiteSpace(proseById))
            {
                Console.Error.WriteLine("[edit-beat] Prose file is empty.");
                return 1;
            }
            var wb  = services.GetRequiredService<NodeWorkbenchService>();
            var dbf = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            var sessionSvc = services.GetRequiredService<EditSessionService>();

            // Capture prior version/hash so we can log the edit to the session synchronously —
            // the workbench's own logging is fire-and-forget and gets dropped when the CLI exits.
            int priorVersion; string? priorHash;
            await using (var db = await dbf.CreateDbContextAsync())
            {
                var b = await db.Beats.AsNoTracking().FirstOrDefaultAsync(x => x.Id == beatId);
                if (b == null) { Console.Error.WriteLine($"[edit-beat] Beat {beatId} not found."); return 1; }
                priorVersion = b.Version; priorHash = b.TextHash;
            }

            Console.Write($"[edit-beat] Updating beat {beatId}… ");
            await wb.UpdateBeatTextAsync(beatId, proseById, expectedUpdatedAt: null);
            await sessionSvc.TryLogBeatAsync(beatId, priorVersion, priorHash);   // synchronous — reliably logged
            Console.WriteLine($"ok ({proseById.Length} chars).");
            return 0;
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

        var dbFactory  = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench  = services.GetRequiredService<NodeWorkbenchService>();

        // Resolve node
        Guid nodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug);
            if (node == null) { Console.Error.WriteLine($"[edit-beat] Node '{slug}' not found."); return 1; }
            nodeId = node.Id;
        }

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);

        if (insertMode)
        {
            // Resolve the anchor's REAL owning chapter node, not the raw --slug node — for a
            // book whose beats live on a child chapter (the normal Book->Chapter->Beat shape),
            // InsertBeatAsync needs that chapter's id or it can't find the anchor at all (same
            // bug class fixed in SetBeatEnabledCli 2026-08-31: VIGL's beats live on its chapter
            // node, not the book node --slug resolves to).
            Guid? afterId = null;
            Guid insertNodeId = nodeId; // top-of-book fallback when --insert-after 0
            if (insertAfter > 0)
            {
                if (insertAfter > ordered.Count)
                {
                    Console.Error.WriteLine($"[edit-beat] --insert-after {insertAfter} exceeds beat count ({ordered.Count}).");
                    return 1;
                }
                var anchor = ordered[insertAfter - 1];
                afterId = anchor.Beat.Id;
                insertNodeId = anchor.NodeId;
            }
            var newBeat = await workbench.InsertBeatAsync(insertNodeId, afterId, prose);
            Console.WriteLine($"[edit-beat] Inserted new beat after position {insertAfter} (chapter {insertNodeId}) → id {newBeat.Id} ({prose.Length} chars).");
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
